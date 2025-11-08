using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Syntax;
namespace Tests;

public class FlowTests
{
    private static IGlykonError[] Check(string src, string file)
    {
        SourceText source = new(file, src);
        var (tokens, _) = new Lexer(source, file).Execute();
        var (syntaxTree, parseErr) = new Parser(tokens, file).Execute();
        
        SemanticAnalyzer semanticAnalyzer = new(syntaxTree, new IdentifierInterner(), file);
        var (_, _, _, errors) = semanticAnalyzer.Analyze();
        
        Assert.Empty(parseErr);

        return errors;
    }

    [Fact]
    public void JumpStatementInsideLoop_ShouldSucceed()
    {
        const string code = """
            while true {
                if false {
                    continue
                }
                break
            }
        """;
        var errors = Check(code, nameof(JumpStatementInsideLoop_ShouldSucceed));
        Assert.Empty(errors);
    }

    [Fact]
    public void JumpStatementOutsideLoop_ShouldFail_WithFlowErrors()
    {
        const string code = """
            while true {
                break       # valid
            }

            continue        # Error: cannot 'continue' outside of a loop

            if true {
                break       # Error: cannot 'break' outside of a loop
            }
        """;

        var errors = Check(code, nameof(JumpStatementOutsideLoop_ShouldFail_WithFlowErrors));
        Assert.Equal(2, errors.Length);
        Assert.All(errors, e => Assert.IsType<FlowError>(e));
    }

    [Fact]
    public void ReturnStatementOutsideFunction_ShouldFail_WithFlowError()
    {
        const string code = "return 42";
        var errors = Check(code, nameof(ReturnStatementOutsideFunction_ShouldFail_WithFlowError));
        var flow = errors.OfType<FlowError>().ToList();
        Assert.Single(flow);
        Assert.Equal(1, errors.Length);
    }

    [Fact]
    public void ReturnInsideTopLevelLoop_ShouldStillFail_WithFlowError()
    {
        const string code = """
            while true {
                return 1   # still outside any function
            }
        """;
        var errors = Check(code, nameof(ReturnInsideTopLevelLoop_ShouldStillFail_WithFlowError));
        Assert.Single(errors);
        Assert.IsType<FlowError>(errors[0]);
    }

    [Fact]
    public void ReturnWithoutValue_InVoidFunction_ShouldSucceed()
    {
        const string code = """
            def process() {
                return
            }
        """;
        var errors = Check(code, nameof(ReturnWithoutValue_InVoidFunction_ShouldSucceed));
        Assert.Empty(errors);
    }

    [Fact]
    public void BreakInsideNestedLocalFunction_WithinOuterLoop_ShouldFail()
    {
        const string code = """
            while true {
                def inner() {
                    break    # Error: no loop in this function context
                }
            }
        """;
        var errors = Check(code, nameof(BreakInsideNestedLocalFunction_WithinOuterLoop_ShouldFail));
        Assert.Single(errors);
        Assert.IsType<FlowError>(errors[0]);
    }

    [Fact]
    public void BreakInsideLocalFunctionOwnLoop_ShouldSucceed()
    {
        const string code = """
            def inner() {
                while true {
                    break
                }
            }
        """;
        var errors = Check(code, nameof(BreakInsideLocalFunctionOwnLoop_ShouldSucceed));
        Assert.Empty(errors);
    }

    [Fact]
    public void ContinueOutsideLoopInIf_ShouldFail_WithFlowError()
    {
        const string code = """
            if true {
                continue   # Error: no enclosing loop
            }
        """;
        var errors = Check(code, nameof(ContinueOutsideLoopInIf_ShouldFail_WithFlowError));
        Assert.Single(errors);
        Assert.IsType<FlowError>(errors[0]);
    }
}
