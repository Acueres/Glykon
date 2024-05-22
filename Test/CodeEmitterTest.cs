using System.Reflection;
using Tython;

namespace Test
{
    public class CodeEmitterTest
    {
        [Fact]
        public void PrintTest()
        {
            const string filename = "HelloTython";
            const string src = @"
            print ""Hello Tython"";
";
            Lexer lexer = new(src, filename);
            var (tokens, _) = lexer.ScanSource();

            Parser parser = new(tokens, filename);
            var (stmts, _) = parser.Parse();

            var codeEmitter = new CodeEmitter(stmts, filename);
            codeEmitter.EmitAssembly();
            using var stream = codeEmitter.ToStream();

            var assemblyData = stream.ToArray();

            Type program = Assembly.Load(assemblyData).GetType($"{filename}.Program");
            var main = program.GetMethod("Main", []);
            main.Invoke(null, []);
        }
    }
}

