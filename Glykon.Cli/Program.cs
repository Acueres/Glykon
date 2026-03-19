using Glykon.Runtime;

namespace Glykon.Cli;

internal static class Program
{
    private static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = """
                            def main( ) { let x: real = 5.0; let y: int = x as int; println(y as str) ; }
                           """;
        GlykonRuntime runtime = new(src, filename);
        var result = runtime.RunAppInMemory();
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        Console.Write(result.Stdout);
    }
}
