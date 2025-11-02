using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;

namespace Tests;

public class ConstantFoldingTests
{
    // Build
    
    private static (List<IRStatement> stmts, IGlykonError[] errors, IdentifierInterner interner) BuildAndFold(string src,
        string file)
    {
        SourceText source = new(file, src);
        var (tokens, lexerErrors) = new Lexer(source, file).Execute();
        var (syntaxTree, parseErr) = new Parser(tokens, file).Execute();
        
        IdentifierInterner interner = new();
        var analyzer = new SemanticAnalyzer(syntaxTree, interner, file);
        var (irTree, _, _, errors) = analyzer.Analyze();
        
        Assert.Empty(lexerErrors);
        Assert.Empty(parseErr);
 
        return (irTree.Select(s => s).ToList(), errors, interner);
    }

    private static IRVariableDeclaration GetVar(IRStatement s)
        => Assert.IsType<IRVariableDeclaration>(s);

    private static IRLiteralExpr GetLit(IRExpression e)
        => Assert.IsType<IRLiteralExpr>(e);

    [Fact]
    public void IntArithmetic()
    {
        const string code = "let x = 2 + 3 * 4";
        var (folded, errors, interner) = BuildAndFold(code, nameof(IntArithmetic));
        Assert.Empty(errors);

        var decl = GetVar(folded.Single());
        Assert.Equal("x", interner[decl.Symbol.NameId]);

        var lit = GetLit(decl.Initializer);
        // Expect 2 + 3*4 = 14
        Assert.Equal(14, lit.Value.Int);
    }

    [Fact]
    public void BoolShortCircuitAnd()
    {
        // The RHS would be a division by zero if evaluated, the folder should short-circuit to false
        const string code = "let x = false and (1 / 0) == 0";
        var (folded, errors, _) = BuildAndFold(code, nameof(BoolShortCircuitAnd));
        Assert.Single(errors);
        
        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Initializer);
        Assert.False(lit.Value.Bool);
    }

    [Fact]
    public void BoolShortCircuitOr()
    {
        const string code = "let x = true or (1 / 0) == 0";
        var (folded, errors, _) = BuildAndFold(code, nameof(BoolShortCircuitOr));
        Assert.Single(errors);

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Initializer);
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
        var (folded, errors, _) = BuildAndFold(code, nameof(Comparisons));
        Assert.Empty(errors);
        
        var a = GetVar(folded[0]);
        Assert.True(GetLit(a.Initializer).Value.Bool);

        var b = GetVar(folded[1]);
        Assert.True(GetLit(b.Initializer).Value.Bool);

        var c = GetVar(folded[2]);
        Assert.False(GetLit(c.Initializer).Value.Bool);
    }

    [Fact]
    public void StringConcat()
    {
        const string code = """
                                let s = 'ab' + "cd"
                                let ok = 'xy' == "xy"
                                let no = 'p' + 'q' == 'pqz' # comment to ensure correct string scanning
                            """;
        var (folded, errors, _) = BuildAndFold(code, nameof(StringConcat));
        Assert.Empty(errors);
        
        var s = GetVar(folded[0]);
        var sLit = GetLit(s.Initializer);
        Assert.Equal(ConstantKind.String, sLit.Value.Kind);
        Assert.Equal("abcd", sLit.Value.String);

        var ok = GetVar(folded[1]);
        Assert.True(GetLit(ok.Initializer).Value.Bool);

        var no = GetVar(folded[2]);
        Assert.False(GetLit(no.Initializer).Value.Bool);
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
        var (folded, errors, interner) = BuildAndFold(code, nameof(PruneToThen));
        Assert.Empty(errors);
        
        var block = Assert.IsType<IRBlockStmt>(folded.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("then_only", interner[decl.Symbol.NameId]);
        Assert.Equal(1, GetLit(decl.Initializer).Value.Int);
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
        var (folded, errors, interner) = BuildAndFold(code, nameof(PruneToElse));
        Assert.Empty(errors);
        
        var block = Assert.IsType<IRBlockStmt>(folded.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("elseOnly", interner[decl.Symbol.NameId]);
        Assert.Equal(2, GetLit(decl.Initializer).Value.Int);
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
        var (folded, errors, interner) = BuildAndFold(code, nameof(WhilePruneToEmptyBlock));
        Assert.Empty(errors);
        
        Assert.Equal(2, folded.Count);
        var first = Assert.IsType<IRBlockStmt>(folded[0]);
        Assert.Empty(first.Statements);

        var y = GetVar(folded[1]);
        Assert.Equal("y", interner[y.Symbol.NameId]);
        Assert.Equal(2, GetLit(y.Initializer).Value.Int);
    }

    [Fact]
    public void RealArithmetic()
    {
        const string code = "let r = 1.5 + 2.5";
        var (folded, errors, _) = BuildAndFold(code, nameof(RealArithmetic));
        Assert.Empty(errors);

        var decl = GetVar(folded.Single());
        var lit = GetLit(decl.Initializer);

        Assert.Equal(ConstantKind.Real, lit.Value.Kind);
        Assert.Equal(4.0, lit.Value.Real, precision: 5);
    }
    
    [Fact]
    public void IntToRealConversion()
    {
        const string code = "let r = 1 + 3.14";
        var (folded, errors, _) =
            BuildAndFold(code, nameof(IntToRealConversion));
        Assert.Empty(errors);

        var decl = GetVar(folded.Single());
        Assert.IsType<IRLiteralExpr>(decl.Initializer);
        Assert.Equal(ConstantKind.Real, ((IRLiteralExpr)decl.Initializer).Value.Kind);
    }
}