using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Flow;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics;

public class SemanticAnalyzer(SyntaxTree syntaxTree, IdentifierInterner interner, string fileName)
{
    readonly SemanticBinder binder = new(syntaxTree, interner, fileName);

    public (BoundTree, SymbolTable, List<IGlykonError>) Analyze()
    {
        var (boundTree, st) = binder.Bind();

        FlowAnalyzer flowAnalyzer = new(boundTree, fileName);

        flowAnalyzer.AnalyzeFlow();

        var typeErrors = binder.GetErrors();
        var refinerErrors = flowAnalyzer.GetErrors();

        var errors = typeErrors.Concat(refinerErrors);
        return (boundTree, st, [.. errors]);
    }
}
