using Glykon.Compiler.Backend.CIL;
using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Core;

namespace Glykon.Cli;

internal class Program
{
    static void Main(string[] args)
    {
        const string filename = "Test";
        const string src = @"
            def main(v: int) {
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
";
        List<IGlykonError> errors = [];
        
        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, lexerErrors) = lexer.Execute();

        errors.AddRange(lexerErrors);
        
        Parser parser = new(tokens, filename);
        var (syntaxTree, parserErrors) = parser.Execute();

        errors.AddRange(parserErrors);
        
        IdentifierInterner interner = new();
        SemanticAnalyzer semanticAnalyzer = new(syntaxTree, interner, filename);
        var (irTree, typeSystem, symbolTable, semanticErrors) = semanticAnalyzer.Analyze();

        errors.AddRange(semanticErrors);

        foreach (var error in errors)
        {
            error.Report();
        }

        if (errors.Count != 0) return;

        var backend = new CilBackend(filename, interner);

        var assembly = backend.Emit(irTree, symbolTable, typeSystem);

        Type? program = assembly.GetType("Program");
        if (program is null) return;

        var main = program.GetMethod("main", [typeof(int)]);
        if (main is null) return;

        main.Invoke(null, [42]);
    }
}
