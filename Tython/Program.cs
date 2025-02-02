using System.Reflection;
using Tython.Component;

namespace Tython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string filename = "Test";
            const string src = @"
            def main() {
                let a = 1
                let b = 2
                add(a, b)
            }

            def add(a: int, b: int) {
                print a + b
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

            if (lexerErrors.Count != 0 || parserErrors.Count != 0) return;

            var generator = new CodeGenerator(stmts, symbolTable, filename);
            generator.GenerateAssembly();

            Assembly assembly = generator.GetAssembly();

            Type program = assembly.GetType("Program");
            var main = program.GetMethod("main", []);
            main.Invoke(null, []);
        }
    }
}
