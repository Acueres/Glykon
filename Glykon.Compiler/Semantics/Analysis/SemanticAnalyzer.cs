using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Flow;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Analysis;

public class SemanticAnalyzer(SyntaxTree syntaxTree, IdentifierInterner interner, string fileName)
{
    readonly SemanticBinder binder = new(syntaxTree, interner, fileName);

    public (BoundTree, SymbolTable, List<IGlykonError>) Analyze()
    {
        var (boundTree, st) = binder.Bind();

        FlowAnalyzer flowAnalyzer = new(boundTree, fileName);
        var flowErrors = flowAnalyzer.Analyze();
        
        ConstantFolder folder = new();
        var foldedTree = folder.Fold(boundTree);
        
        var typeErrors = binder.GetErrors();
        var errors = typeErrors.Concat(flowErrors);
        
        return (foldedTree, st, [.. errors]);
    }
}
