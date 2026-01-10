namespace Glykon.Compiler.Diagnostics.Errors
{
    public class SyntaxError(int line, string filename, string message) : IGlykonError
    {
        private readonly int line = line;
        private readonly string filename = filename;
        private readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}, line {line})");
        }
    }
}
