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
                                println((a + b) as str)
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

    // Custom Types / Fields / Member Access / Methods

    [Fact]
    public void Script_CustomType_FieldInitializer_AndMemberAccess()
    {
        const string code = """
                                class Point {
                                    x: int = 5
                                }

                                let p = new Point {}
                                println(p.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_CustomType_FieldInitializer_AndMemberAccess));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("5" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_CustomType_FieldAssignment_Works()
    {
        const string code = """
                                class Point {
                                    x: int
                                }

                                let p = new Point { x: 5 }
                                p.x = 7
                                println(p.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_CustomType_FieldAssignment_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("7" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_CustomType_InstanceMethod_UsesFields()
    {
        const string code = """
                                class Counter {
                                    value: int = 0

                                    def inc(self) {
                                        self.value = self.value + 1
                                    }

                                    def get(self) -> int {
                                        return self.value
                                    }
                                }

                                let c = new Counter {}
                                c.inc()
                                c.inc()
                                c.inc()
                                println(c.get() as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_CustomType_InstanceMethod_UsesFields));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("3" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_CustomType_MethodTakesCustomTypeParam()
    {
        const string code = """
                                class B { value: int = 7 }

                                class A {
                                    def take(b: B) -> int {
                                        return b.value
                                    }
                                }

                                let a = new A {}
                                let b = new B {}
                                println(a.take(b) as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_CustomType_MethodTakesCustomTypeParam));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("7" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_CustomType_MethodReturnsCustomType()
    {
        const string code = """
                                class B { value: int = 9 }

                                class A {
                                    def makeB() -> B {
                                        return new B {}
                                    }
                                }

                                let a = new A {}
                                let b = a.makeB()
                                println(b.value as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_CustomType_MethodReturnsCustomType));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("9" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_AssociatedConst_AccessibleOnType()
    {
        const string code = """
                                class Math {
                                    const pi: real = 3.14
                                }

                                println(Math.pi as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_AssociatedConst_AccessibleOnType));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Contains("3.14", result.Stdout);
    }

    [Fact]
    public void Script_ObjectInit_DefaultsAndOverride_Works()
    {
        const string code = """
                                class A {
                                    x: int = 5
                                    y: int = 7
                                }

                                let a = new A { y: 10 }
                                println(a.x as str)
                                println(a.y as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectInit_DefaultsAndOverride_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("5" + Environment.NewLine + "10" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ObjectInit_DefaultFromAssociatedConst_Works()
    {
        const string code = """
                                class A {
                                    const k: int = 4
                                    x: int = k
                                }

                                let a = new A {}
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectInit_DefaultFromAssociatedConst_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("4" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ObjectInit_ChainedInnerObject_Works()
    {
        const string code = """
                                class B { v: int = 2 }
                                class A { b: B }

                                let a = new A { b: new B { v: 11 } }
                                println(a.b.v as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectInit_ChainedInnerObject_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("11" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ObjectInit_UsedInIfCondition_Works()
    {
        const string code = """
                                class P { x: int }

                                if new P { x: 0 }.x == 0 {
                                    println('ok')
                                } else {
                                    println('bad')
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectInit_UsedInIfCondition_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("ok" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ObjectConstructorResolution_Works()
    {
        const string code = """
                                class A {
                                    f: int
                                    
                                    def init(f: int) -> A {
                                        return new A { f: f }
                                    }
                                }

                                let a = A(42)
                                println(a.f as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectConstructorResolution_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("42" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ObjectConstructorResolution_WrongReturnType_Errors()
    {
        const string code = """
                                class B { x: int }
                                class A {
                                    f: int
                                    
                                    def init(f: int) -> B {
                                        return new B { x: f }
                                    }
                                }

                                let a = A(42)
                                println(a.f as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ObjectConstructorResolution_WrongReturnType_Errors));
        Assert.Throws<InvalidOperationException>(() => runtime.RunScript());
    }

    [Fact]
    public void Script_NestedType_ConstructorResolution_Works()
    {
        const string code = """
                                class Outer {
                                    class Inner {
                                        v: int

                                        def init(v: int) -> Inner {
                                            return new Inner { v: v }
                                        }
                                    }
                                }

                                let x = Outer.Inner(7)
                                println(x.v as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_NestedType_ConstructorResolution_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("7" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ExplicitInitCall_Works()
    {
        const string code = """
                                class A {
                                    x: int

                                    def init(x: int) -> A {
                                        return new A { x: x }
                                    }
                                }

                                let a = A.init(9)
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ExplicitInitCall_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("9" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_ConstructorCall_ArgCoercion_Works()
    {
        const string code = """
                                class A {
                                    x: real

                                    def init(x: real) -> A {
                                        return new A { x: x }
                                    }
                                }

                                let a = A(3)         # int -> real coercion
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ConstructorCall_ArgCoercion_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Contains("3", result.Stdout); // formatting "3" vs "3.0"
    }

    [Fact]
    public void Script_ConstructorCall_InMemberChain_Works()
    {
        const string code = """
                                class A {
                                    x: int
                                    def init(x: int) -> A { return new A { x: x } }
                                }

                                if A(0).x == 0 {
                                    println('ok')
                                } else {
                                    println('bad')
                                }
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_ConstructorCall_InMemberChain_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("ok" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_NestedType_ObjectInit_Works()
    {
        const string code = """
                                class Outer {
                                    class Inner {
                                        x: int = 2
                                    }
                                }

                                let i = new Outer.Inner {}
                                println(i.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_NestedType_ObjectInit_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("2" + Environment.NewLine, result.Stdout);
    }

    // Value vs reference types behavior

    [Fact]
    public void Script_Struct_AssignmentCopies_Value()
    {
        const string code = """
                                struct S { x: int }

                                let a = new S { x: 1 }
                                let b = a
                                b.x = 2

                                println(a.x as str)
                                println(b.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_AssignmentCopies_Value));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("1" + Environment.NewLine + "2" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Class_AssignmentCopies_Reference()
    {
        const string code = """
                                class C { x: int }

                                let a = new C { x: 1 }
                                let b = a
                                b.x = 2

                                println(a.x as str)
                                println(b.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Class_AssignmentCopies_Reference));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("2" + Environment.NewLine + "2" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_PassedToFunction_IsCopied()
    {
        const string code = """
                                struct S { x: int }

                                def bump(s: S) {
                                    s.x = s.x + 1
                                }

                                let a = new S { x: 10 }
                                bump(a)
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_PassedToFunction_IsCopied));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("10" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Class_PassedToFunction_IsSameObject()
    {
        const string code = """
                                class C { x: int }

                                def bump(c: C) {
                                    c.x = c.x + 1
                                }

                                let a = new C { x: 10 }
                                bump(a)
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Class_PassedToFunction_IsSameObject));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("11" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_FieldMutation_OnLocal_Works()
    {
        const string code = """
                                struct S { x: int }

                                let s = new S { x: 3 }
                                s.x = 9
                                println(s.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_FieldMutation_OnLocal_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("9" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_NestedMemberAccess_Works()
    {
        const string code = """
                                struct P { x: int }
                                struct L { p: P }

                                let l = new L { p: new P { x: 7 } }
                                println(l.p.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_NestedMemberAccess_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("7" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_FieldRead_FromTemporary_Works()
    {
        const string code = """
                                struct S { x: int }

                                def make(v: int) -> S {
                                    return new S { x: v }
                                }

                                println(make(5).x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_FieldRead_FromTemporary_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("5" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_FieldWrite_ToTemporary_ShouldError()
    {
        const string code = """
                                struct S { x: int }

                                def make(v: int) -> S {
                                    return new S { x: v }
                                }

                                make(1).x = 2
                                println('unreachable')
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_FieldWrite_ToTemporary_ShouldError));

        Assert.ThrowsAny<Exception>(() => runtime.RunScript());
    }

    // Struct methods
    [Fact]
    public void Script_Struct_Method_ReadsField_Works()
    {
        const string code = """
                                struct S {
                                    x: int

                                    def get(this) -> int {
                                        return this.x
                                    }
                                }

                                let s = new S { x: 5 }
                                println(s.get() as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_Method_ReadsField_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("5" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_Struct_Method_MutatesField_Persists()
    {
        const string code = """
                                struct S {
                                    x: int

                                    def inc(this) {
                                        this.x = this.x + 1
                                    }
                                }

                                let s = new S { x: 1 }
                                s.inc()
                                s.inc()
                                println(s.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_Method_MutatesField_Persists));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("3" + Environment.NewLine, result.Stdout);
    }
    
    [Fact]
    public void Script_Struct_Method_MutatesCopy_DoesNotAffectOriginal()
    {
        const string code = """
                                struct S {
                                    x: int
                                    def inc(this) { this.x = this.x + 1 }
                                }

                                let a = new S { x: 10 }
                                let b = a
                                b.inc()

                                println(a.x as str)
                                println(b.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_Method_MutatesCopy_DoesNotAffectOriginal));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("10" + Environment.NewLine + "11" + Environment.NewLine, result.Stdout);
    }
    
    [Fact]
    public void Script_Struct_Method_CallOnTemporary_ReadOnly_Works()
    {
        const string code = """
                                struct S {
                                    x: int
                                    def get(this) -> int { return this.x }
                                }

                                def make(v: int) -> S { return new S { x: v } }

                                println(make(7).get() as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_Struct_Method_CallOnTemporary_ReadOnly_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("7" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_MethodReceiverName_CanBeThis()
    {
        const string code = """
                                class A {
                                    x: int = 3

                                    def get(this) -> int {
                                        return this.x
                                    }
                                }

                                let a = new A {}
                                println(a.get() as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_MethodReceiverName_CanBeThis));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Equal("3" + Environment.NewLine, result.Stdout);
    }

    [Fact]
    public void Script_FieldDefault_ImplicitConversion_IntToReal_Works()
    {
        const string code = """
                                class A {
                                    x: real = 3
                                }

                                let a = new A {}
                                println(a.x as str)
                            """;

        var runtime = new GlykonRuntime(code, nameof(Script_FieldDefault_ImplicitConversion_IntToReal_Works));
        var result = runtime.RunScript();

        Assert.Null(result.Exception);
        Assert.Contains("3", result.Stdout);
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
                                    println(i as str)
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
                                    println(i as str)
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
                                    println(i as str)
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
                                    println(i as str)
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
                                    println(i as str)
                                }
                            """;

        var runtime =
            new GlykonRuntime(code, nameof(Script_For_DescendingInclusiveWithNegativeStep_Prints10DownTo0By2));
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
                                    println(i as str)
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
                                    println(i as str)
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
                                    println(i as str)
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
                                    println(i as str)
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