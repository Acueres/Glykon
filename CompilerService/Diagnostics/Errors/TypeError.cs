namespace CompilerService.Diagnostics.Errors
{
    public class TypeError(string filename, string message) : IGlykonError
    {
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}");
        }
    }
}
