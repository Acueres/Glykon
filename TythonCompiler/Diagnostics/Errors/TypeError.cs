namespace TythonCompiler.Diagnostics.Errors
{
    public class TypeError(string filename, string message) : ITythonError
    {
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}");
        }
    }
}
