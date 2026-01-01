using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Types;
using Tests.Infrastructure;

namespace Tests;

public class SemanticTests : CompilerTestBase
{
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
            def ping() { return }
            (ping)()   # grouping around identifier is allowed
        ";

        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }
    
    [Fact]
    public void InstanceMethodCall_WithCorrectArgs_Succeeds()
    {
        const string src = """
                               class Vec {
                                   def add(self, x: int) -> int { return x + 1 }
                               }

                               def use(v: Vec) -> int {
                                   return v.add(41)
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
    }
    
    [Fact]
    public void InstanceMethodCall_WrongArgType_Fails()
    {
        const string src = """
                               class Vec {
                                   def add(self, x: int) -> int { return x + 1 }
                               }

                               def use(v: Vec) -> int {
                                   return v.add('no')
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Single(r.AllErrors);
    }
    
    [Fact]
    public void MemberAccess_UnknownMember_Fails()
    {
        const string src = """
                               class A { }

                               def f(a: A) {
                                   a.nope
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Single(r.AllErrors);
    }
    
    [Fact]
    public void NestedType_QualifiedName_Resolves()
    {
        const string src = """
                               class Outer {
                                   class Inner { }
                               }

                               def f(x: Outer.Inner) { return }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
    }
    
    [Fact]
    public void FieldInitializer_TypeMismatch_Fails()
    {
        const string src = """
                               class A {
                                   x: int = 'oops'
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Single(r.AllErrors);
    }
    
    [Fact]
    public void AssociatedConst_TypeMismatch_Fails()
    {
        const string src = """
                               class Math {
                                   const pi: real = 'oops'
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Single(r.AllErrors);
    }
    
    [Fact]
    public void MemberAccess_Field_Succeeds()
    {
        const string src = """
                               class Point { x: int }

                               def f(p: Point) -> int {
                                   return p.x
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
    }
    
    [Fact]
    public void FieldAssignment_TypeMismatch_Fails()
    {
        const string src = """
                               class Point { x: int }

                               def f(p: Point) {
                                   p.x = 'oops'
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Single(r.AllErrors);
    }

    [Fact]
    public void InstanceMethodCall_Succeeds()
    {
        const string src = """
                               class A {
                                   def inc(self, x: int) -> int { return x + 1 }
                               }

                               def f(a: A) -> int {
                                   return a.inc(41)
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
    }
    
    [Fact]
    public void StaticMethodCall_Succeeds()
    {
        const string src = """
                               class A {
                                   def make(x: int) -> int { return x }
                               }

                               def f() -> int {
                                   return A.make(1)
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
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
