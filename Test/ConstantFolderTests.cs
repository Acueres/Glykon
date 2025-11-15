using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Tests.Infrastructure;

namespace Tests;

public class ConstantFoldingTests : CompilerTestBase
{
    [Fact]
    public void IntArithmetic()
    {
        const string src = "let x = 2 + 3 * 4";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var decl = GetVar(semanticResult.Ir.Single());
        Assert.Equal("x", semanticResult.Interner[decl.Symbol.NameId]);

        var lit = GetLit(decl.Initializer);
        // Expect 2 + 3*4 = 14
        Assert.Equal(14, lit.Value.Int);
    }

    [Fact]
    public void BoolShortCircuitAnd()
    {
        // The RHS would be a division by zero if evaluated, the folder should short-circuit to false
        const string src = "let x = false and (1 / 0) == 0";
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Single(semanticResult.AllErrors);

        var decl = GetVar(semanticResult.Ir.Single());
        var lit = GetLit(decl.Initializer);
        Assert.False(lit.Value.Bool);
    }

    [Fact]
    public void BoolShortCircuitOr()
    {
        const string src = "let x = true or (1 / 0) == 0";
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Single(semanticResult.AllErrors);

        var decl = GetVar(semanticResult.Ir.Single());
        var lit = GetLit(decl.Initializer);
        Assert.True(lit.Value.Bool);
    }

    [Fact]
    public void Comparisons()
    {
        const string src = """
                                let a = 2 < 3
                                let b = 3 == 3
                                let c = 5 >= 10
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var folded = semanticResult.Ir;

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
        const string src = """
                                let s = 'ab' + "cd"
                                let ok = 'xy' == "xy"
                                let no = 'p' + 'q' == 'pqz' # comment to ensure correct string scanning
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);
        
        var folded = semanticResult.Ir;

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
        const string src = """
                                if true {
                                    let then_only = 1
                                }
                                else {
                                    let never = 2
                                }
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var block = Assert.IsType<IRBlockStmt>(semanticResult.Ir.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("then_only", semanticResult.Interner[decl.Symbol.NameId]);
        Assert.Equal(1, GetLit(decl.Initializer).Value.Int);
    }

    [Fact]
    public void PruneToElse()
    {
        const string src = """
                                if false {
                                    let never = 1
                                } else {
                                    let elseOnly = 2
                                }
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var block = Assert.IsType<IRBlockStmt>(semanticResult.Ir.Single());
        var decl = GetVar(block.Statements.Single());
        Assert.Equal("elseOnly", semanticResult.Interner[decl.Symbol.NameId]);
        Assert.Equal(2, GetLit(decl.Initializer).Value.Int);
    }

    [Fact]
    public void WhilePruneToEmptyBlock()
    {
        const string src = """
                                while false {
                                    let never = 1
                                }
                                let y = 2
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);
        
        var folded = semanticResult.Ir;

        Assert.Equal(2, folded.Length);
        var first = Assert.IsType<IRBlockStmt>(folded[0]);
        Assert.Empty(first.Statements);

        var y = GetVar(folded[1]);
        Assert.Equal("y", semanticResult.Interner[y.Symbol.NameId]);
        Assert.Equal(2, GetLit(y.Initializer).Value.Int);
    }

    [Fact]
    public void RealArithmetic()
    {
        const string src = "let r = 1.5 + 2.5";
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var decl = GetVar(semanticResult.Ir.Single());
        var lit = GetLit(decl.Initializer);

        Assert.Equal(ConstantKind.Real, lit.Value.Kind);
        Assert.Equal(4.0, lit.Value.Real, precision: 5);
    }

    [Fact]
    public void IntToRealConversion()
    {
        const string src = "let r = 1 + 3.14";
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var decl = GetVar(semanticResult.Ir.Single());
        Assert.IsType<IRLiteralExpr>(decl.Initializer);
        Assert.Equal(ConstantKind.Real, ((IRLiteralExpr)decl.Initializer).Value.Kind);
    }

    [Fact]
    public void ConstPinAndInlineReal()
    {
        const string src = """
                                const pi: real = 3.14
                                let a = pi
                                let b = 2 * pi
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
        
        var folded  = semanticResult.Ir;
        var interner = semanticResult.Interner;

        var piDecl = GetConst(folded[0]);
        var piInit = GetLit(piDecl.Initializer);
        Assert.Equal("pi", interner[piDecl.Symbol.NameId]);
        Assert.Equal(ConstantKind.Real, piInit.Value.Kind);
        Assert.Equal(3.14, piInit.Value.Real, precision: 5);
        Assert.Equal(ConstantKind.Real, piDecl.Symbol.Value.Kind);
        Assert.Equal(3.14, piDecl.Symbol.Value.Real, precision: 5);

        var a = GetVar(folded[1]);
        Assert.Equal("a", interner[a.Symbol.NameId]);
        Assert.Equal(3.14, GetLit(a.Initializer).Value.Real, precision: 5);

        var b = GetVar(folded[2]);
        Assert.Equal("b", interner[b.Symbol.NameId]);
        Assert.Equal(6.28, GetLit(b.Initializer).Value.Real, precision: 5);
    }

    [Fact]
    public void ConstChainedIntFoldAndInline()
    {
        const string src = """
                                const x: int = 2 + 3
                                const y: int = x * 4
                                let z = y + 1
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
        
        var folded = semanticResult.Ir;
        var interner = semanticResult.Interner;
        
        var x = GetConst(folded[0]);
        Assert.Equal("x", interner[x.Symbol.NameId]);
        Assert.Equal(5, GetLit(x.Initializer).Value.Int);
        Assert.Equal(5, x.Symbol.Value.Int);

        var y = GetConst(folded[1]);
        Assert.Equal("y", interner[y.Symbol.NameId]);
        Assert.Equal(20, GetLit(y.Initializer).Value.Int);
        Assert.Equal(20, y.Symbol.Value.Int);

        var z = GetVar(folded[2]);
        Assert.Equal("z", interner[z.Symbol.NameId]);
        Assert.Equal(21, GetLit(z.Initializer).Value.Int);
    }

    [Fact]
    public void ConstBeforeUseRequired_NotFoldedOnForwardRef()
    {
        const string src = """
                                const a: int = b + 1
                                const b: int = 2
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);

        // We allow any diagnostic policy the folder chose; but at minimum, a's init should NOT fold.
        Assert.NotEmpty(semanticResult.AllErrors);

        var a = GetConst(semanticResult.Ir[0]);
        Assert.Equal("a", semanticResult.Interner[a.Symbol.NameId]);
        Assert.IsNotType<IRLiteralExpr>(a.Initializer); // not a literal due to forward reference
    }

    [Fact]
    public void ConstIntPromotesIntoRealFold()
    {
        const string src = """
                                const i: int = 2
                                const r: real = i + 3.0
                                let s = r * 2
                            """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
        
        var folded = semanticResult.Ir;
        var interner = semanticResult.Interner;

        var i = GetConst(folded[0]);
        Assert.Equal("i", interner[i.Symbol.NameId]);
        Assert.Equal(2, GetLit(i.Initializer).Value.Int);
        Assert.Equal(2, i.Symbol.Value.Int);

        var r = GetConst(folded[1]);
        Assert.Equal("r", interner[r.Symbol.NameId]);
        var rLit = GetLit(r.Initializer);
        Assert.Equal(ConstantKind.Real, rLit.Value.Kind);
        Assert.Equal(5.0, rLit.Value.Real, precision: 5);
        Assert.Equal(ConstantKind.Real, r.Symbol.Value.Kind);
        Assert.Equal(5.0, r.Symbol.Value.Real, precision: 5);

        var s = GetVar(folded[2]);
        var sLit = GetLit(s.Initializer);
        Assert.Equal(ConstantKind.Real, sLit.Value.Kind);
        Assert.Equal(10.0, sLit.Value.Real, precision: 5);
    }
}