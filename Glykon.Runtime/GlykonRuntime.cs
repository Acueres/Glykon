using System.Reflection;
using System.Runtime.Loader;

using Glykon.Compiler.Backend.CIL;
using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Syntax;

namespace Glykon.Runtime;

public sealed class GlykonRuntime(string source, string fileName)
{
    public ExecutionResult RunScript(params object?[]? args)
    {
        var compileResult = CompileToMemory(LanguageMode.Script);
        
        return RunMain(compileResult, args);
    }
    
    public ExecutionResult RunAppInMemory(params object?[]? args)
    {
        var compileResult = CompileToMemory(LanguageMode.Application);
        return RunMain(compileResult, args);
    }
    
    public CompileResult CompileToMemory(LanguageMode mode)
    {
        var semanticResult = Compile(source, mode, fileName, out var errors);
        if (errors.Length != 0)
        {
            throw new InvalidOperationException(
                "Compilation failed:\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }
        
        var backend = new CilBackend(semanticResult, new AssemblyName(fileName));
        
        var emittedImage = backend.EmitToImage();
        
        return new CompileResult(
            emittedImage.Assembly,
            semanticResult,
            errors,
            AssemblyLoadContext.Default,
            emittedImage.Functions
        );
    }

    public BuildResult BuildApp(string outputDir)
    {
        var semanticResult = Compile(source, LanguageMode.Application, fileName, out var errors);
        if (errors.Length != 0)
        {
            throw new InvalidOperationException(
                "Compilation failed:\n" + string.Join("\n", errors.Select(e => e.ToString())));
        }
        
        var backend = new CilBackend(semanticResult, new AssemblyName(fileName));
        return backend.EmitToDisk(outputDir);
    }

    public ExecutionResult RunMain(CompileResult compiled, params object?[]? args)
    {
        if (compiled.Assembly?.EntryPoint is not null)
        {
            return ExecuteMethod(compiled.Assembly.EntryPoint, null, args);
        }
        
        return InvokeByName(compiled, "main", null, args);
    }

    public ExecutionResult InvokeByName(
        CompileResult compiled,
        string functionName,
        string? containerType,
        params object?[]? args)
    {
        if (compiled.Assembly is null)
            return new ExecutionResult(null, "", new InvalidOperationException("No assembly due to prior errors."));
        
        var method = FindMethod(compiled.Assembly, functionName, containerType);
        
        if (method is null)
            return new ExecutionResult(null, "", new MissingMethodException($"Function '{functionName}' not found."));

        return ExecuteMethod(method, null, args);
    }

    private ExecutionResult ExecuteMethod(MethodInfo method, object? instance, object?[]? args)
    {
        var original = Console.Out;
        using var sw = new StringWriter();
        Console.SetOut(sw);
        object? ret = null;
        Exception? ex = null;

        try
        {
            ret = method.Invoke(instance, args ?? []);
        }
        catch (TargetInvocationException tie)
        {
            ex = tie.InnerException ?? tie;
        }
        catch (Exception e)
        {
            ex = e;
        }
        finally
        {
            Console.SetOut(original);
        }

        return new ExecutionResult(ret, sw.ToString(), ex);
    }

    private static MethodInfo? FindMethod(Assembly assembly, string name, string? type)
    {
        foreach (var t in assembly.GetTypes())
        {
            if (type != null && t.Name != type) continue;
            
            var m = t.GetMethod(name,
                    BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic,
                    binder: null, types: Type.EmptyTypes, modifiers: null)
                 ?? t.GetMethod(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            
            if (m is null)
            {
                 m = t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                      .FirstOrDefault(x => x.Name == name);
            }

            if (m is not null) return m;
        }
        return null;
    }

    private static SemanticResult Compile(string src, LanguageMode mode, string file, out IGlykonError[] errors)
    {
        var text = new SourceText(file, src);

        var lexer = new Lexer(text, file);
        var lexResult = lexer.Lex();

        var parser = new Parser(lexResult, file);
        var parseResult = parser.Parse();
        
        var semanticAnalyzer = new SemanticAnalyzer(parseResult, mode, file);
        var semanticResult = semanticAnalyzer.Analyze();

        errors = semanticResult.AllErrors.ToArray();

        return semanticResult;
    }
}