using Glykon.Runtime;

namespace Glykon.Cli;

internal static class Program
{
    static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = """

                                       def main() {
                                            for i in 0..21 {
                                                println(fib(i));
                                            }
                                       }
                                       
                                       def fib(n: int) -> int {
                                            let a = 0;
                                            let b = 1;
                                            let i = 0;
                           
                                            while i < n {
                                                let next = a + b;
                                                a = b;
                                                b = next;
                                                i = i + 1;
                                            }
                           
                                        return a;
                                      }
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
