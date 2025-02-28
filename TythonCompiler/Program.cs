using System.Reflection;

using TythonCompiler.Tokenization;
using TythonCompiler.Parsing;
using TythonCompiler.SemanticRefinement;
using TythonCompiler.CodeGeneration;

namespace TythonCompiler
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string filename = "Test";
            const string src = @"
            def main() {
                let i = 0

                while i < 20 {
                    println(fib(i))
                    i = i + 1
                }
            }

            def fib(n: int) -> int {
                if n <= 1 {
                    return n
                }

                return fib(n - 2) + fib(n - 1)
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

            SemanticRefiner refiner = new(stmts, symbolTable, filename);
            var refinerErrors = refiner.Execute();

            if (lexerErrors.Count != 0 || parserErrors.Count != 0 || refinerErrors.Count != 0) return;

            var generator = new TypeGenerator(stmts, symbolTable, filename);
            generator.GenerateAssembly();

            Assembly assembly = generator.GetAssembly();

            Type program = assembly.GetType("Program");
            var main = program.GetMethod("main", []);
            main.Invoke(null, []);
        }
    }
}
