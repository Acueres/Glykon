using System.Diagnostics;

using Glykon.Compiler.Core;
using Glykon.Runtime;

namespace Tests;

public class RuntimeTests
{
    // Script Mode Tests (Top Level Statements)

    [Fact]
    public void Script_HelloWorld_CapturesStdout()
    {
        const string code = """
            println('Hello Glykon!')
        """;

        var runtime = new GlykonRuntime(code, nameof(Script_HelloWorld_CapturesStdout));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("Hello Glykon!" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_VariableCalculation()
    {
        const string code = """
            let a = 10
            let b = 20
            println(a + b)
        """;

        var runtime = new GlykonRuntime(code, nameof(Script_VariableCalculation));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Contains("30", result.Stdout);
    }
    
    // Application Mode Tests (Requires Main)

    [Fact]
    public void App_InMemory_RunsMain()
    {
        const string code = """
            def main() {
                println('App Mode Active')
            }
        """;
        
        var runtime = new GlykonRuntime(code, nameof(App_InMemory_RunsMain));
        var result = runtime.RunAppInMemory();

        Assert.Null(result.Exception);
        Assert.Contains("App Mode Active", result.Stdout);
    }

    [Fact]
    public void App_MissingMain_ThrowsCompilationError()
    {
        const string code = """
            def not_main() { }
        """;

        var runtime = new GlykonRuntime(code, nameof(App_MissingMain_ThrowsCompilationError));
        
        Assert.Throws<InvalidOperationException>(() => runtime.RunAppInMemory());
    }
    
    // Separate Function Invocation Tests

    [Fact]
    public void Invoke_AddFunction_ReturnsResult()
    {
        const string code = """
            def add(a: int, b: int) -> int {
                return a + b
            }
            # Top level code is allowed in Script mode, but we ignore it and call 'add' directly
            println('ignored')
        """;

        var runtime = new GlykonRuntime(code, nameof(Invoke_AddFunction_ReturnsResult));
        
        var compiled = runtime.CompileToMemory(LanguageMode.Script);
        
        var result = runtime.InvokeByName(compiled, "add", null, 5, 7);

        Assert.Null(result.Exception);
        Assert.Equal(12L, result.ReturnValue);
    }

    [Fact]
    public void Invoke_FunctionCallsOtherFunction()
    {
        const string code = """
            def wrapper(msg: str) {
                log(msg)
            }
            def log(s: str) {
                println('Log: ' + s)
            }
        """;
        
        var runtime = new GlykonRuntime(code, nameof(Invoke_FunctionCallsOtherFunction));
        var compiled = runtime.CompileToMemory(LanguageMode.Script);

        var result = runtime.InvokeByName(compiled, "wrapper", null, "test");

        Assert.Null(result.Exception);
        Assert.Contains("Log: test", result.Stdout);
    }
    
    // Built Assembly Tests

    [Fact]
    public void Disk_BuildAndRun_Executable()
    {
        const string code = """
            def main() {
                println('From Disk')
            }
        """;
        string testName = nameof(Disk_BuildAndRun_Executable);
        string outputDir = Path.Combine(Path.GetTempPath(), "GlykonTests", testName);
        
        var runtime = new GlykonRuntime(code, testName);
        var buildResult = runtime.BuildApp(outputDir);

        Assert.True(File.Exists(buildResult.DllPath), "DLL should exist");
        Assert.True(File.Exists(buildResult.RuntimeConfigPath), "Runtime config should exist");
        
        var (exitCode, stdout, stderr) = RunDotNetProcess(buildResult.DllPath);

        Assert.Equal(0, exitCode);
        Assert.Contains("From Disk", stdout);
        
        if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
    }

    [Fact]
    public void Disk_Execution_HandlesRuntimeErrors()
    {
        // Assuming Glykon has a panic/throw mechanism or we cause a .NET exception (div by zero)
        // If not, valid Glykon code that crashes.
        const string code = """
            def main() {
                 let zero = 0
                 let x = 1 / zero
            }
        """;
        string testName = nameof(Disk_Execution_HandlesRuntimeErrors);
        string outputDir = Path.Combine(Path.GetTempPath(), "GlykonTests", testName);

        var runtime = new GlykonRuntime(code, testName);
        runtime.BuildApp(outputDir);
        string dllPath = Path.Combine(outputDir, testName + ".dll");

        var (exitCode, stdout, stderr) = RunDotNetProcess(dllPath);
        
        Assert.NotEqual(0, exitCode);
        Assert.NotEmpty(stderr);

        if (Directory.Exists(outputDir)) Directory.Delete(outputDir, true);
    }
    
    // For loops tests
    [Fact]
    public void Script_For_AscendingExclusive_Prints0To9()
    {
        const string code = """
                                for i in 0..10 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_AscendingExclusive_Prints0To9));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 0; i < 10; i++)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }
    
    [Fact]
    public void Script_For_AscendingInclusive_Prints0To10()
    {
        const string code = """
                                for i in 0..=10 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_AscendingInclusive_Prints0To10));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 0; i <= 10; i++)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }
    
    [Fact]
    public void Script_For_AscendingWithStep_PrintsEvenNumbers()
    {
        const string code = """
                                for i in 0..10 by 2 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_AscendingWithStep_PrintsEvenNumbers));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 0; i < 10; i += 2)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }
    
    [Fact]
    public void Script_For_DescendingExclusiveWithNegativeStep_Prints10DownTo1()
    {
        const string code = """
                                for i in 10..0 by -1 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_DescendingExclusiveWithNegativeStep_Prints10DownTo1));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 10; i > 0; i--)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }
    
    [Fact]
    public void Script_For_DescendingInclusiveWithNegativeStep_Prints10DownTo0By2()
    {
        const string code = """
                                for i in 10..=0 by -2 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_DescendingInclusiveWithNegativeStep_Prints10DownTo0By2));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 10; i >= 0; i -= 2)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }

    [Fact]
    public void Script_For_StepZero_ProducesNoOutput()
    {
        const string code = """
                                for i in 0..10 by 0 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_StepZero_ProducesNoOutput));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal(string.Empty, result.Stdout);
    }
    
    [Fact]
    public void Script_For_StartGreaterThanEnd_DefaultStep_ProducesNoOutput()
    {
        const string code = """
                                for i in 10..0 {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_StartGreaterThanEnd_DefaultStep_ProducesNoOutput));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal(string.Empty, result.Stdout);
    }
    
    [Fact]
    public void Script_For_DynamicPositiveStep_Works()
    {
        const string code = """
                                let step = 2
                                for i in 0..10 by step {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_DynamicPositiveStep_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 0; i < 10; i += 2)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }
    
    [Fact]
    public void Script_For_DynamicNegativeStep_Works()
    {
        const string code = """
                                let step = -2
                                for i in 10..0 by step {
                                    println(i)
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_For_DynamicNegativeStep_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);

        var expected = "";
        for (int i = 10; i > 0; i -= 2)
        {
            expected += i + Environment.NewLine;
        }

        Assert.Equal(expected, result.Stdout);
    }

    // Helpers

    private static (int ExitCode, string Stdout, string Stderr) RunDotNetProcess(string dllPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"\"{dllPath}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi) 
            ?? throw new InvalidOperationException("Failed to start dotnet process.");
        
        var stdout = process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return (process.ExitCode, stdout, stderr);
    }
}