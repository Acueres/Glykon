using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Tests.Infrastructure;

namespace Tests;

public class TypeTests : CompilerTestBase
{
    // Helpers

    private IGlykonError[] Check(string src, string file)
    {
        var semanticResult = Analyze(src, LanguageMode.Script, file);
        return [..semanticResult.AllErrors];
    }

    [Fact]
    public void VariableTypeInference()
    {
        const string src = """

                                       let i = 6
                                       let res = i + (2 + 2 * 3)

                           """;

        var semanticResult = Analyze(src, LanguageMode.Script);
        var irTree = semanticResult.Ir;
        var interner = semanticResult.Interner;

        Assert.Empty(semanticResult.AllErrors);
        Assert.NotEmpty(irTree);

        var f = GetFunction(irTree.Single());

        Assert.Equal(2, f.Body.Statements.Length);
        Assert.Equal(IRStatementKind.Variable, f.Body.Statements[1].Kind);
        var stmt = (IRVariableDeclaration)f.Body.Statements[1];

        string name = interner[stmt.Symbol.NameId];
        Assert.Equal("res", name);
        Assert.NotNull(stmt.Initializer);
        Assert.Equal(TypeKind.Int64, stmt.Symbol.Type.Kind);
    }

    [Fact]
    public void VariableWrongTypeInference()
    {
        const string src = """

                                       let res = (2 + 2 * 'text')
                           """;
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Single(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        Assert.Single(f.Body.Statements);
    }

    [Fact]
    public void CastLiteral()
    {
        const string src = """
                                       println(42 as str)
                           """;
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        var exprStmt = GetExprStmt(f.Body.Statements.Single());
        var call = GetCall(exprStmt.Expression);
        var cast = call.Parameters.Single();
        Assert.Equal(TypeKind.String, cast.Type.Kind);
    }

    [Fact]
    public void CastVariable()
    {
        const string src = """

                                       let f = 2.74
                                       let i = f as int
                           """;
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        var varDec = GetVar(f.Body.Statements[1]);
        Assert.Equal(TypeKind.Int64, varDec.Initializer.Type.Kind);
    }

    // Literals, unary & binary
    [Fact]
    public void UnaryAndLiteralSuccess()
    {
        const string code = """
                                let i = 5
                                let r = -i
                                let f = true
                                let g = not f
                            """;
        Assert.Empty(Check(code, nameof(UnaryAndLiteralSuccess)));
    }

    [Fact]
    public void UnaryTypeMismatch()
    {
        const string code = """
                                let s = 'text'
                                let oops = -s      # string cannot be negated
                            """;
        Assert.Single(Check(code, nameof(UnaryTypeMismatch)));
    }

    [Fact]
    public void BinaryArithmeticSuccess()
    {
        const string code = """
                                let a = 2 + 3 * 4
                                let b = a / 2 - 1
                            """;
        Assert.Empty(Check(code, nameof(BinaryArithmeticSuccess)));
    }

    [Fact]
    public void BinaryArithmeticTypeMismatch()
    {
        const string code = """
                                let x = 10 + 'str'   # int + string is illegal
                            """;
        Assert.Single(Check(code, nameof(BinaryArithmeticTypeMismatch)));
    }

    [Fact]
    public void LogicalAndComparisonChecks()
    {
        const string ok = """
                              let a = (2 < 3) and true
                          """;
        Assert.Empty(Check(ok, nameof(LogicalAndComparisonChecks)));

        const string bad = """
                               let b = (2 < 3) and 4 # rhs not bool
                           """;
        Assert.Single(Check(bad, nameof(LogicalAndComparisonChecks) + "_bad"));
    }

    // Variable and constant declarations
    [Fact]
    public void ExplicitVariableTypeMatch()
    {
        const string code = """
                                let i: int = 42
                            """;
        Assert.Empty(Check(code, nameof(ExplicitVariableTypeMatch)));
    }

    [Fact]
    public void ExplicitVariableTypeMismatch()
    {
        const string code = """
                                let s: str = 123
                            """;
        Assert.Single(Check(code, nameof(ExplicitVariableTypeMismatch)));
    }

    [Fact]
    public void ConstantTypeMismatch()
    {
        const string code = """
                                const Pi: real = 'oops'  # const must match declared type
                            """;
        Assert.Single(Check(code, nameof(ConstantTypeMismatch)));
    }

    // Variable and constant assignments
    [Fact]
    public void AssignValueToVariable()
    {
        const string code = """
                                let i: int = 42
                                i = 0
                            """;
        Assert.Empty(Check(code, nameof(AssignValueToVariable)));
    }

    [Fact]
    public void AssignValueToImmutableVariable()
    {
        const string code = """
                                let const i: int = 42
                                i = 0
                            """;
        Assert.Single((Check(code, nameof(AssignValueToImmutableVariable))));
    }

    [Fact]
    public void AssignValueToConstant()
    {
        const string code = """
                                const i: int = 42
                                i = 0
                            """;
        Assert.Single((Check(code, nameof(AssignValueToConstant))));
    }

    //Conditions

    [Fact]
    public void IfWhileConditionMustBeBool()
    {
        const string code = """
                                if 0 { let a = 1 }
                                while 'text' { break }
                            """;
        Assert.Equal(2, Check(code, nameof(IfWhileConditionMustBeBool)).Count());
    }
    
    // Expression statements
    
    [Fact]
    public void NonVoidExpressionStatement_Fails()
    {
        const string src = """
                               class A { x: int }
                               def f() {
                                   new A { x: 1 }
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.NotEmpty(r.AllErrors);
    }
    
    [Fact]
    public void VoidCallExpressionStatement_Succeeds()
    {
        const string src = """
                               class A {
                                   x: int = 0
                                   def inc(self) { self.x = self.x + 1 }
                               }

                               def f(a: A) {
                                   a.inc()
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);
    }
    
    

    // Function returns
    
    [Fact]
    public void FunctionReturnTypeMatch()
    {
        const string code = """
                                def add(a: int, b: int) -> int {
                                    return a + b
                                }
                            """;
        Assert.Empty(Check(code, nameof(FunctionReturnTypeMatch)));
    }

    [Fact]
    public void FunctionReturnTypeMismatch()
    {
        const string code = """
                                def bad() -> int {
                                    return 'str'
                                }
                            """;
        Assert.Single(Check(code, nameof(FunctionReturnTypeMismatch)));
    }

    [Fact]
    public void VoidFunctionReturningValue()
    {
        const string code = """
                                def nope() {
                                    return 1
                                }
                            """;
        Assert.Single(Check(code, nameof(VoidFunctionReturningValue)));
    }

    [Fact]
    public void ReturnWithoutValueFromTypedFunction_ShouldFail_WithTypeError()
    {
        const string code = """
                                def get_value() -> int {
                                    return
                                }
                            """;
        var errors = Check(code, nameof(ReturnWithoutValueFromTypedFunction_ShouldFail_WithTypeError));

        Assert.Single(errors);
        Assert.All(errors, e => Assert.IsType<TypeError>(e));
    }

    // Types
    [Fact]
    public void MemberAccess_HasFieldType()
    {
        const string src = """
                               class Point { x: int }

                               def f(p: Point) -> int {
                                   return p.x
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var ret = (IRReturnStmt)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Return);
        Assert.Equal(TypeKind.Int64, ret.Value!.Type.Kind);
    }

    [Fact]
    public void VariableInference_FromMemberAccess()
    {
        const string src = """
                               class Point { x: int }

                               def f(p: Point) {
                                   let y = p.x
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var decl = (IRVariableDeclaration)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Variable);
        Assert.Equal(TypeKind.Int64, decl.Symbol.Type.Kind);
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

        var errors = Check(src, nameof(FieldAssignment_TypeMismatch_Fails));
        Assert.Single(errors);
    }

    [Fact]
    public void MethodCall_ReturnTypePropagates()
    {
        const string src = """
                               class A {
                                   def get() -> int { return 7 }
                               }

                               def f(a: A) -> int {
                                   return a.get()
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var ret = (IRReturnStmt)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Return);
        Assert.Equal(TypeKind.Int64, ret.Value!.Type.Kind);
    }

    [Fact]
    public void IRMemberAccess_FieldType_IsInt()
    {
        const string src = """
                               class Point { x: int }

                               def f(p: Point) -> int {
                                   return p.x
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var ret = (IRReturnStmt)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Return);

        Assert.Equal(TypeKind.Int64, ret.Value!.Type.Kind);
    }
    
    [Fact]
    public void FieldDefault_RefersToSelf_Fails()
    {
        const string code = """
                                class A {
                                    b: int = self.a
                                    a: int = 5
                                }
                            """;

        Assert.NotEmpty(Check(code, nameof(FieldDefault_RefersToSelf_Fails)));
    }
    
    [Fact]
    public void FieldDefault_RefersToThis_Fails()
    {
        const string code = """
                                class A {
                                    b: int = this.a
                                    a: int = 5
                                }
                            """;

        Assert.NotEmpty(Check(code, nameof(FieldDefault_RefersToThis_Fails)));
    }
    
    [Fact]
    public void FieldDefault_RefersToOtherFieldByName_Fails()
    {
        const string code = """
                                class A {
                                    a: int = 5
                                    b: int = a
                                }
                            """;

        Assert.NotEmpty(Check(code, nameof(FieldDefault_RefersToOtherFieldByName_Fails)));
    }

    [Fact]
    public void Method_ThisParameter_AllowsMemberAccess()
    {
        const string code = """
                                class A {
                                    a: int

                                    def m(this) -> int {
                                        return this.a
                                    }
                                }
                            """;

        Assert.Empty(Check(code, nameof(Method_ThisParameter_AllowsMemberAccess)));
    }
    
    
    [Fact]
    public void Method_ThisParameter_DoesNotIntroduceSelf()
    {
        const string code = """
                                class A {
                                    a: int

                                    def m(this) -> int {
                                        return self.a
                                    }
                                }
                            """;

        Assert.NotEmpty(Check(code, nameof(Method_ThisParameter_DoesNotIntroduceSelf)));
    }

    [Fact]
    public void VariableInference_FromCustomTypedField_UsesCustomType()
    {
        const string src = """
                               class Node { next: Node }

                               def f(n: Node) {
                                   let x = n.next
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var decl = (IRVariableDeclaration)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Variable);

        var typeName = r.Interner[decl.Symbol.Type.NameId];
        Assert.Equal("Node", typeName);
    }

    [Fact]
    public void IRCall_InstanceMethod_InsertsReceiverAsFirstArg()
    {
        const string src = """
                               class A { def m(self, x: int) -> int { return x } }

                               def f(a: A) -> int {
                                   return a.m(1)
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var ret = (IRReturnStmt)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Return);

        var call = (IRCallExpr)ret.Value!;
        Assert.IsType<MethodSymbol>(call.Callable);

        Assert.True(call.Parameters.Length >= 1);
        Assert.Equal(IRExpressionKind.Name, call.Parameters[0].Kind); // receiver is "a"
    }

    [Fact]
    public void IRCall_StaticMethod_DoesNotInsertReceiver()
    {
        const string src = """
                               class A { def make(x: int) -> int { return x } }

                               def f() -> int {
                                   return A.make(1)
                               }
                           """;

        var r = Analyze(src, LanguageMode.Script);
        Assert.Empty(r.AllErrors);

        var f = GetFunction(
            r.Ir.Single(s => s is IRFunctionDeclaration func && r.Interner[func.Signature.NameId] == "f"));
        var ret = (IRReturnStmt)f.Body.Statements.Single(s => s.Kind == IRStatementKind.Return);

        var call = (IRCallExpr)ret.Value!;
        Assert.IsType<MethodSymbol>(call.Callable);

        // Only the explicit arg should be present (no receiver injection)
        Assert.Single(call.Parameters);
    }
}
