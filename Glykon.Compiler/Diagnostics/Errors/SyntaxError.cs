namespace Glykon.Compiler.Diagnostics.Errors
{
    public class SyntaxError(int line, string filename, string message) : IGlykonError
    {
        readonly int line = line;
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}, line {line})");
        }
    }
}
