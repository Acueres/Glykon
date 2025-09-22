using Glykon.Compiler.Semantics;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;

namespace Tests;

public class ConstantFoldingTests
{
    // Build
    private static (BoundTree bound, BoundStatement[] folded, IdentifierInterner interner) BuildAndFold(string src,
        string file)
    {
        var (tokens, errors) = new Lexer(src, file).Execute();
        var (syntaxTree, parseErr) = new Parser(tokens, file).Execute();
        Assert.Empty(parseErr);

        IdentifierInterner interner = new();
        var analyzer = new SemanticAnalyzer(syntaxTree, interner, file);
        var (boundTree, _, semErr) = analyzer.Analyze();
        Assert.Empty(semErr);

        var folder = new ConstantFolder(boundTree);
        var foldedTree = folder.Fold();
        return (boundTree, foldedTree.Select(x => x).ToArray(), interner);
    }

    private static BoundVariableDeclaration GetVar(BoundStatement s)
        => Assert.IsType<BoundVariableDeclaration>(s);

    private static BoundLiteralExpr GetLit(BoundExpression e)
        => Assert.IsType<BoundLiteralExpr>(e);

    [Fact]
    public void IntArithmetic()
    {
        const string code = "let x = 2 + 3 * 4";
        var (_, folded, interner) = BuildAndFold(code, nameof(IntArithmetic));

        var decl = GetVar(folded.Single());
        Assert.Equal("x", interner[decl.Symbol.Id]);

        var lit = GetLit(decl.Expression);
        // Expect 2 + 3*4 = 14
        Assert.Equal(14, lit.Token.IntValue);
    }

    [Fact]
    public void BoolShortCircuitAnd()
    {
        // The RHS would be a division by zero if evaluated, the folder should short-circuit to false
        const string code = "let x = false and (1 / 0) == 0";
        var (_, folded, _) = BuildAndFold(code, nameof(BoolShortCircuitAnd));
        
        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);
        Assert.Equal(TokenKind.LiteralFalse, lit.Token.Kind);
    }

    [Fact]
    public void BoolShortCircuitOr()
    {
        const string code = "let x = true or (1 / 0) == 0";
        var (_, folded, _) = BuildAndFold(code, nameof(BoolShortCircuitOr));

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);
        Assert.Equal(TokenKind.LiteralTrue, lit.Token.Kind);
    }

    [Fact]
    public void Comparisons()
    {
        const string code = """
                                let a = 2 < 3
                                let b = 3 == 3
                                let c = 5 >= 10
                            """;
        var (_, folded, _) = BuildAndFold(code, nameof(Comparisons));

        var a = GetVar(folded[0]);
        Assert.Equal(TokenKind.LiteralTrue, GetLit(a.Expression).Token.Kind);

        var b = GetVar(folded[1]);
        Assert.Equal(TokenKind.LiteralTrue, GetLit(b.Expression).Token.Kind);

        var c = GetVar(folded[2]);
        Assert.Equal(TokenKind.LiteralFalse, GetLit(c.Expression).Token.Kind);
    }

    [Fact]
    public void StringConcat()
    {
        const string code = """
                                let s = 'ab' + "cd"
                                let ok = 'xy' == "xy"
                                let no = 'p' + 'q' == 'pqz' # comment to ensure correct string scanning
                            """;
        var (_, folded, _) = BuildAndFold(code, nameof(StringConcat));

        var s = GetVar(folded[0]);
        var sLit = GetLit(s.Expression);
        Assert.Equal(TokenKind.LiteralString, sLit.Token.Kind);
        Assert.Equal("abcd", sLit.Token.StringValue);

        var ok = GetVar(folded[1]);
        Assert.Equal(TokenKind.LiteralTrue, GetLit(ok.Expression).Token.Kind);

        var no = GetVar(folded[2]);
        Assert.Equal(TokenKind.LiteralFalse, GetLit(no.Expression).Token.Kind);
    }

    [Fact]
    public void PruneToThen()
    {
        const string code = """
                                if true {
                                    let then_only = 1
                                }
                                else {
                                    let never = 2
                                }
                            """;
        var (_, folded, interner) = BuildAndFold(code, nameof(PruneToThen));

        var block = Assert.IsType<BoundBlockStmt>(folded.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("then_only", interner[decl.Symbol.Id]);
        Assert.Equal(1, GetLit(decl.Expression).Token.IntValue);
    }

    [Fact]
    public void PruneToElse()
    {
        const string code = """
                                if false {
                                    let never = 1
                                } else {
                                    let elseOnly = 2
                                }
                            """;
        var (_, folded, interner) = BuildAndFold(code, nameof(PruneToElse));

        var block = Assert.IsType<BoundBlockStmt>(folded.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("elseOnly", interner[decl.Symbol.Id]);
        Assert.Equal(2, GetLit(decl.Expression).Token.IntValue);
    }

    [Fact]
    public void WhilePruneToEmptyBlock()
    {
        const string code = """
                                while false {
                                    let never = 1
                                }
                                let y = 2
                            """;
        var (_, folded, interner) = BuildAndFold(code, nameof(WhilePruneToEmptyBlock));

        Assert.Equal(2, folded.Length);
        var first = Assert.IsType<BoundBlockStmt>(folded[0]);
        Assert.Empty(first.Statements);

        var y = GetVar(folded[1]);
        Assert.Equal("y", interner[y.Symbol.Id]);
        Assert.Equal(2, GetLit(y.Expression).Token.IntValue);
    }

    [Fact]
    public void RealArithmetic()
    {
        const string code = "let r = 1.5 + 2.5";
        var (_, folded, _) = BuildAndFold(code, nameof(RealArithmetic));

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);

        Assert.Equal(TokenKind.LiteralReal, lit.Token.Kind);
        Assert.Equal(4.0, lit.Token.RealValue, precision: 5);
    }
    
    // TODO: Add implicit conversion int -> real
    /*[Fact]
    public void IntToRealConversion()
    {
        const string code = "let r = 1 + 3.14";
        var (_, folded, _) =
            BuildAndFold(code, nameof(IntToRealConversion));

        var decl = GetVar(folded.Single());
        Assert.IsType<BoundBinaryExpr>(decl.Expression);
    }*/
}