using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.IR.Statements;
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
                                       42 as str
                           """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);
        
        var f = GetFunction(semanticResult.Ir.Single());
        var exprStmt = GetExprStmt(f.Body.Statements.Single());
        Assert.Equal(TypeKind.String, exprStmt.Expression.Type.Kind);
    }
    
    [Fact]
    public void CastVariable()
    {
        const string src = """

                                       let f = 2.74
                                       f as int
                           """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        var exprStmt = GetExprStmt(f.Body.Statements[1]);
        Assert.Equal(TypeKind.Int64, exprStmt.Expression.Type.Kind);
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
}
