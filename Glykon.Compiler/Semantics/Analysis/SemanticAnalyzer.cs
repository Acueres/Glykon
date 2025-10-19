using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Flow;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Analysis;

public class SemanticAnalyzer(SyntaxTree syntaxTree, IdentifierInterner interner, string fileName)
{
    public (BoundTree, TypeSystem, SymbolTable, List<IGlykonError>) Analyze()
    {
        TypeSystem typeSystem = new(interner);
        typeSystem.BuildPrimitives();

        SemanticBinder binder = new(syntaxTree, typeSystem, interner, fileName);

        var (boundTree, st) = binder.Bind();

        FlowAnalyzer flowAnalyzer = new(boundTree, fileName);
        var flowErrors = flowAnalyzer.Analyze();
        
        ConstantFolder folder = new();
        var foldedTree = folder.Fold(boundTree);
        
        var typeErrors = binder.GetErrors();
        var errors = typeErrors.Concat(flowErrors);
        
        return (foldedTree, typeSystem, st, [.. errors]);
    }
}
