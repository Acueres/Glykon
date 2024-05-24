namespace Tython
{
    internal class Program
    {
        static void Main(string[] args)
        {
            const string filename = "HelloTython";
            const string src = @"
            print ""Hello Tython""
            print 'This is the first compiled Tython program';";
            Lexer lexer = new(src, filename);
            var (tokens, _) = lexer.ScanSource();

            Parser parser = new(tokens, filename);
            var (stmts, _) = parser.Parse();

            var generator = new CodeGenerator(stmts, filename);

            var assembly = generator.GetAssembly();

            Type program = assembly.GetType("Program");
            var main = program.GetMethod("Main", []);
            main.Invoke(null, []);
        }
    }
}
