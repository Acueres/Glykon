namespace TythonCompiler.Diagnostics.Errors
{
    public class SyntaxError(int line, string filename, string message) : ITythonError
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
