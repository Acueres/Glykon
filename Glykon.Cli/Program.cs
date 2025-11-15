using System.Reflection;

using Glykon.Compiler.Backend.CIL;
using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Core;

namespace Glykon.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = @"
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
";
        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var lexResult = lexer.Lex();
        
        Parser parser = new(lexResult, filename);
        var parseResult = parser.Parse();
        
        IdentifierInterner interner = new();
        SemanticAnalyzer semanticAnalyzer = new(parseResult, interner, LanguageMode.Application, filename);
        var semanticResult = semanticAnalyzer.Analyze();

        var errors = semanticResult.AllErrors.ToArray();

        foreach (var error in errors)
        {
            error.Report();
        }

        if (errors.Length != 0) return;

        var backend = new CilBackend(semanticResult, new AssemblyName(filename), filename);

        var assembly = backend.Emit();

        Type? program = assembly.GetType(filename);
        if (program is null) return;

        var main = program.GetMethod("main", []);
        if (main is null) return;

        main.Invoke(null, []);
    }
}
