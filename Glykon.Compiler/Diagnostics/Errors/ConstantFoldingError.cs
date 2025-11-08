namespace Glykon.Compiler.Diagnostics.Errors;

public class ConstantFoldingError(string filename, string message) : IGlykonError
{
    public void Report()
    {
        Console.WriteLine($"{message} ({filename})");
    }
}