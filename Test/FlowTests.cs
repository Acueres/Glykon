using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Tests.Infrastructure;

namespace Tests;

public class FlowTests : CompilerTestBase
{
    [Fact]
    public void JumpStatementInsideLoop_ShouldSucceed()
    {
        const string src = """
            while true {
                if false {
                    continue
                }
                break
            }
        """;
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void JumpStatementOutsideLoop_ShouldFail_WithFlowErrors()
    {
        const string src = """
            while true {
                break       # valid
            }

            continue        # Error: cannot 'continue' outside of a loop

            if true {
                break       # Error: cannot 'break' outside of a loop
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Equal(2, semanticResult.AllErrors.Count());
        Assert.All(semanticResult.AllErrors, e => Assert.IsType<FlowError>(e));
    }

    [Fact]
    public void ReturnStatementOutsideFunction_ShouldFail_WithFlowError()
    {
        const string src = "return 42";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Single(semanticResult.AllErrors);
        Assert.All(semanticResult.AllErrors, e => Assert.IsType<FlowError>(e));
    }

    [Fact]
    public void ReturnInsideTopLevelLoop_ShouldStillFail_WithFlowError()
    {
        const string src = """
            while true {
                return 1   # still outside any function
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        
        Assert.Single(semanticResult.AllErrors);
        Assert.All(semanticResult.AllErrors, e => Assert.IsType<FlowError>(e));
    }

    [Fact]
    public void ReturnWithoutValue_InVoidFunction_ShouldSucceed()
    {
        const string src = """
            def process() {
                return
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void BreakInsideNestedLocalFunction_WithinOuterLoop_ShouldFail()
    {
        const string src = """
            while true {
                def inner() {
                    break    # Error: no loop in this function context
                }
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Single(semanticResult.AllErrors);
        Assert.All(semanticResult.AllErrors, e => Assert.IsType<FlowError>(e));
    }

    [Fact]
    public void BreakInsideLocalFunctionOwnLoop_ShouldSucceed()
    {
        const string src = """
            def inner() {
                while true {
                    break
                }
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);
        Assert.Empty(semanticResult.AllErrors);
    }

    [Fact]
    public void ContinueOutsideLoopInIf_ShouldFail_WithFlowError()
    {
        const string src = """
            if true {
                continue   # Error: no enclosing loop
            }
        """;
        
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Single(semanticResult.AllErrors);
        Assert.All(semanticResult.AllErrors, e => Assert.IsType<FlowError>(e));
    }
}
