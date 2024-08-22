using System.Reflection;

namespace Tython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string filename = "Test";
            const string src = @"
            print 'constant evaluation test'
            print (2 * 4) / (2 + 2 * 3.0)

            print 'arithmetic test'
            let f = 4.7;
            let i = 5;
            print f + f
            print f * f
            print f / f
            print f - f
            print i + i
            print i * i
            print i - i
            print i / i

            print 'string ' + 'concatenation test'
            
            print 'logical operations test'
            print i == i
            print i != i
            print 6 > i
            print 5 >= i
            print 6 < i
            print 5 <= i
";
            Lexer lexer = new(src, filename);
            var (tokens, lexerErrors) = lexer.Execute();

            foreach (var error  in lexerErrors)
            {
                error.Report();
            }

            Parser parser = new(tokens, filename);
            var (stmts, symbolTable, parserErrors) = parser.Execute();

            foreach (var error in parserErrors)
            {
                error.Report();
            }

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
