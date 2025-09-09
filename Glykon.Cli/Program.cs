using System.Reflection;

using Glykon.Compiler.Emitter;
using Glykon.Compiler.Semantics;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = @"
            def main() {
                let i = 0

                while i < 20
                {
                    println(fib(i))
                    i = i + 1
                }

                def fib(n: int) -> int {
                    #println(i)
                    if n <= 1 {
                        return n
                    }

                    return fib(n - 2) + fib(n - 1)
                }
            }
";
        List<IGlykonError> errors = [];
        
        Lexer lexer = new(src, filename);
        var (tokens, lexerErrors) = lexer.Execute();

        errors.AddRange(lexerErrors);

        IdentifierInterner interner = new();
        Parser parser = new(tokens, filename);
        var (syntaxTree, parserErrors) = parser.Execute();

        errors.AddRange(parserErrors);

        SemanticBinder binder = new(syntaxTree, interner);
        SymbolTable symbolTable = binder.Bind();

        SemanticAnalyzer semanticAnalyzer = new(syntaxTree, symbolTable, filename);
        var semanticErrors = semanticAnalyzer.Execute();

        errors.AddRange(semanticErrors);

        foreach (var error in errors)
        {
            error.Report();
        }

        if (errors.Count != 0) return;

        var emitter = new TypeEmitter(syntaxTree, symbolTable, interner, filename);
        emitter.EmitAssembly();

        Assembly assembly = emitter.GetAssembly();

        Type? program = assembly.GetType("Program");
        if (program is null) return;

        var main = program.GetMethod("main", []);
        if (main is null) return;

        main.Invoke(null, []);
    }
}
