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
            let a = 0
            let temp = 0
            let b = 1

            while a < 10000 {
                print a
                if a == 8 {
                    break
                }
                temp = a
                a = b
                b = temp + b
            }

            let i = 0
            while i < 10 {
                i = i + 1
                if i == 5 {
                    continue
                }
                print i
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

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            var generator = new CodeGenerator(optimizedStmts, symbolTable, filename);

            Assembly assembly = generator.GetAssembly();

            Type program = assembly.GetType("Program");
            var main = program.GetMethod("Main", []);
            main.Invoke(null, []);
        }
    }
}
