using Glykon.Runtime;

namespace Glykon.Cli;

internal static class Program
{
    static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = """

                                       def main() {
                                           println(v)
                                           def inner() {
                                               let i = 1
                                               if true {
                                                   let k = 2.5
                                                   {
                                                       println(i + k)
                                                   }
                                               }
                                           }
                                           inner() # Should print 3.5
                                       }
                                       const v: int = 7 + 4
                           """;
        GlykonRuntime runtime = new(src, filename);
        var result = runtime.RunAppInMemory();
        
        if (result.Exception is not null)
        {
            throw result.Exception;
        }
        
        Console.Write(result.Stdout);

        runtime.BuildApp("out");
    }
}
