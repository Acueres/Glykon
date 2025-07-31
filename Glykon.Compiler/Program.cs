using System.Reflection;
using Glykon.Compiler.Emitter;
using Glykon.Compiler.Semantics;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler;

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
        Lexer lexer = new(src, filename);
        var (tokens, lexerErrors) = lexer.Execute();

        foreach (var error in lexerErrors)
        {
            error.Report();
        }

        IdentifierInterner interner = new();
        Parser parser = new(tokens, interner, filename);
        var (stmts, symbolTable, parserErrors) = parser.Execute();

        foreach (var error in parserErrors)
        {
            error.Report();
        }

        SemanticAnalyzer semanticAnalyzer = new(stmts, symbolTable, filename);
        var semanticErrors = semanticAnalyzer.Execute();

        foreach (var error in semanticErrors)
        {
            error.Report();
        }

        if (lexerErrors.Count != 0 || parserErrors.Count != 0 || semanticErrors.Count != 0) return;

        var emitter = new TypeEmitter(stmts, symbolTable, interner, filename);
        emitter.EmitAssembly();

        Assembly assembly = emitter.GetAssembly();

        Type? program = assembly.GetType("Program");
        if (program is null) return;

        var main = program.GetMethod("main", []);
        if (main is null) return;

        main.Invoke(null, []);
    }
}
