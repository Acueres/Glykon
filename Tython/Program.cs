using System.Reflection;

namespace Tython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string filename = "HelloTython";
            const string src = @"
            print ""Hello Tython""
            print 'test' == 'test'
            let text = 'testing variables'
            print text
            print (2 * 4) / (2 + 2 * 3)
";
            Lexer lexer = new(src, filename);
            var (tokens, _) = lexer.Execute();

            Parser parser = new(tokens, filename);
            var (stmts, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            var generator = new CodeGenerator(optimizedStmts, filename);

            Assembly assembly = generator.GetAssembly();

            Type program = assembly.GetType("Program");
            var main = program.GetMethod("Main", []);
            main.Invoke(null, []);
        }
    }
}
