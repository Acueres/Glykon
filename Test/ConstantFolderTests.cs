using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Core;

namespace Tests;

public class ConstantFoldingTests
{
    // Build
    private static (BoundTree bound, BoundStatement[] folded, IdentifierInterner interner) BuildAndFold(string src,
        string file)
    {
        SourceText source = new(file, src);
        var (tokens, errors) = new Lexer(source, file).Execute();
        var (syntaxTree, parseErr) = new Parser(tokens, file).Execute();
        Assert.Empty(parseErr);

        IdentifierInterner interner = new();
        var analyzer = new SemanticAnalyzer(syntaxTree, interner, file);
        var (boundTree, _, semErr) = analyzer.Analyze();
        Assert.Empty(semErr);

        var folder = new ConstantFolder();
        var foldedTree = folder.Fold(boundTree);
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
        Assert.Equal("x", interner[decl.Symbol.NameId]);

        var lit = GetLit(decl.Expression);
        // Expect 2 + 3*4 = 14
        Assert.Equal(14, lit.Value.Int);
    }

    [Fact]
    public void BoolShortCircuitAnd()
    {
        // The RHS would be a division by zero if evaluated, the folder should short-circuit to false
        const string code = "let x = false and (1 / 0) == 0";
        var (_, folded, _) = BuildAndFold(code, nameof(BoolShortCircuitAnd));
        
        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);
        Assert.False(lit.Value.Bool);
    }

    [Fact]
    public void BoolShortCircuitOr()
    {
        const string code = "let x = true or (1 / 0) == 0";
        var (_, folded, _) = BuildAndFold(code, nameof(BoolShortCircuitOr));

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);
        Assert.True(lit.Value.Bool);
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
        Assert.True(GetLit(a.Expression).Value.Bool);

        var b = GetVar(folded[1]);
        Assert.True(GetLit(b.Expression).Value.Bool);

        var c = GetVar(folded[2]);
        Assert.False(GetLit(c.Expression).Value.Bool);
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
        Assert.Equal(ConstantKind.String, sLit.Value.Kind);
        Assert.Equal("abcd", sLit.Value.String);

        var ok = GetVar(folded[1]);
        Assert.True(GetLit(ok.Expression).Value.Bool);

        var no = GetVar(folded[2]);
        Assert.False(GetLit(no.Expression).Value.Bool);
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
        Assert.Equal("then_only", interner[decl.Symbol.NameId]);
        Assert.Equal(1, GetLit(decl.Expression).Value.Int);
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
        Assert.Equal("elseOnly", interner[decl.Symbol.NameId]);
        Assert.Equal(2, GetLit(decl.Expression).Value.Int);
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
        Assert.Equal("y", interner[y.Symbol.NameId]);
        Assert.Equal(2, GetLit(y.Expression).Value.Int);
    }

    [Fact]
    public void RealArithmetic()
    {
        const string code = "let r = 1.5 + 2.5";
        var (_, folded, _) = BuildAndFold(code, nameof(RealArithmetic));

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Expression);

        Assert.Equal(ConstantKind.Real, lit.Value.Kind);
        Assert.Equal(4.0, lit.Value.Real, precision: 5);
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