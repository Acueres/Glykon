using System.Reflection;

using Glykon.Compiler.Tokenization;
using Glykon.Compiler.Parsing;
using Glykon.Compiler.CodeGeneration;
using Glykon.Compiler.SemanticAnalysis;

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

        Parser parser = new(tokens, filename);
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

        var generator = new TypeGenerator(stmts, symbolTable, filename);
        generator.GenerateAssembly();

        Assembly assembly = generator.GetAssembly();

        Type? program = assembly.GetType("Program");
        if (program is null) return;

        var main = program.GetMethod("main", []);
        if (main is null) return;

        main.Invoke(null, []);
    }
}
