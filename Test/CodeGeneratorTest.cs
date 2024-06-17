using Tython;

namespace Test
{
    public class CodeGeneratorTest
    {
        [Fact]
        public void PrintTest()
        {
            const string filename = "HelloTython";
            const string src = @"
            print ""Hello Tython"";
";
            Lexer lexer = new(src, filename);
            var (tokens, _) = lexer.Execute();

            Parser parser = new(tokens, filename);
            var (stmts, _) = parser.Execute();

            var codeGenerator = new CodeGenerator(stmts, filename);

            Type program = codeGenerator.GetAssembly().GetType("Program");
            var main = program.GetMethod("Main", []);
            main.Invoke(null, []);
        }
    }
}

