namespace Glykon.Compiler.Diagnostics.Errors;

public class BindingError(string filename, string message) : IGlykonError
{
    public void Report()
    {
        Console.WriteLine($"{message} ({filename})");
    }
}