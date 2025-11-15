using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Types;
using Tests.Infrastructure;

namespace Tests;

public class SemanticTests : CompilerTestBase
{
    [Fact]
    public void VariableTypeInference()
    {
        const string src = @"
            let i = 6
            let res = i + (2 + 2 * 3)
";

        var semanticResult = Analyze(src, LanguageMode.Script);
        var irTree = semanticResult.Ir;
        var interner = semanticResult.Interner;
        
        Assert.Empty(semanticResult.AllErrors);
        Assert.NotEmpty(irTree);
        Assert.Equal(2, irTree.Length);
        Assert.Equal(IRStatementKind.Variable, irTree[1].Kind);
        var stmt = (IRVariableDeclaration)irTree[1];

        string name = interner[stmt.Symbol.NameId];
        Assert.Equal("res", name);
        Assert.NotNull(stmt.Initializer);
        Assert.Equal(TypeKind.Int64, stmt.Symbol.Type.Kind);
    }

    [Fact]
    public void VariableWrongTypeInference()
    {
        const string src = @"
            let res = (2 + 2 * 'text')
";
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Single(semanticResult.AllErrors);
        Assert.Single(semanticResult.Ir);
    }
    
    [Fact]
    public void CheckVariableInsideConstantDeclaration()
    {
        const string src = """
                                let v = 1
                                const c: int = 5 * v
                            """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Equal(2, semanticResult.SemanticErrors.Length);
    }

    // Calls & overloads

    [Fact]
    public void CallWithCorrectArguments()
    {
        const string src = """
            def sum(a: int, b: int) -> int { return a + b }
            let r = sum(2, 3)
        """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void CallWithWrongArgumentType()
    {
        const string src = """
            def sum(a: int, b: int) -> int { return a + b }
            let r = sum(2, 'str')
        """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Single(semanticResult.AllErrors);
    }

    [Fact]
    public void OverloadResolutionSuccess()
    {
        const string src = """
            def log(msg: str) { return }
            def log(level: int, msg: str) { return }
            log('hi')          # picks 1‑arg
            log(1, 'bye')      # picks 2‑arg
        """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void OverloadResolutionFailure()
    {
        const string src = """
            def log(msg: str) { return }
            def log(level: int, msg: str) { return }
            log(true, 'oops')   # no matching overload
        """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Single(semanticResult.AllErrors);
    }

    [Fact]
    public void CallWithUnknownIdentifier()
    {
        const string src = @"
            foo()      # unknown
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Single(semanticResult.AllErrors);
    }

    [Fact]
    public void CallOnVariableNotCallable()
    {
        const string src = @"
            let x = 1
            x()        # variable, not a function
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Single(semanticResult.AllErrors);
    }

    [Fact]
    public void CallWithParenthesizedIdentifier()
    {
        const string src = @"
            def ping(): return
            (ping)()   # grouping around identifier is allowed
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void CallOverloadNoMatch()
    {
        const string src = @"
            def log(i: int): return
            def log(i: int, j: int): return
            log(true)     # no matching overload for (bool)
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Single(semanticResult.AllErrors);
    }

    [Fact]
    public void CallOverloadExactMatch()
    {
        const string src = @"
            def log(i: int): return
            def log(i: int, j: int): return
            log(1)
            log(1, 2)
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    // Symbol table tests

    [Fact]
    public void QualifiedNameLocalFunction()
    {
        var interner = new IdentifierInterner();

        var typeSystem = new TypeSystem(interner);
        typeSystem.BuildPrimitives();
        var int64 = typeSystem[TypeKind.Int64];

        var table = new SymbolTable(interner);

        var main = table.RegisterFunction("main", int64, []);
        Assert.NotNull(main);

        table.BeginScope(main!);
        var add = table.RegisterFunction("add", int64, [int64, int64]);
        Assert.NotNull(add);

        // Qualified name should be "main.add"
        var simple = interner[add!.NameId];
        var qualified = interner[add.QualifiedNameId];

        Assert.Equal("add", simple);
        Assert.Equal("main.add", qualified);
    }

    [Fact]
    public void QualifiedNameDeeplyNested()
    {
        var interner = new IdentifierInterner();

        var typeSystem = new TypeSystem(interner);
        typeSystem.BuildPrimitives();
        var int64 = typeSystem[TypeKind.Int64];

        var table = new SymbolTable(interner);

        var main = table.RegisterFunction("main", int64, []);
        Assert.NotNull(main);

        table.BeginScope(main!);
        var inner = table.RegisterFunction("inner", int64, []);
        Assert.NotNull(inner);

        table.BeginScope(inner!);
        var add = table.RegisterFunction("add", int64, [int64, int64]);
        Assert.NotNull(add);

        var qualified = interner[add!.QualifiedNameId];
        Assert.Equal("main.inner.add", qualified);
    }

    [Fact]
    public void QualifiedNameTopLevelFunction()
    {
        var interner = new IdentifierInterner();

        var typeSystem = new TypeSystem(interner);
        typeSystem.BuildPrimitives();
        var int64 = typeSystem[TypeKind.Int64];

        var table = new SymbolTable(interner);

        var top = table.RegisterFunction("util", int64, []);
        Assert.NotNull(top);

        var simple = interner[top!.NameId];
        var qualified = interner[top.QualifiedNameId];

        Assert.Equal("util", simple);
        Assert.Equal("util", qualified);
    }
}
