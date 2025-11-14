using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Flow;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Analysis;

public class SemanticAnalyzer(SyntaxTree syntaxTree, IdentifierInterner interner, LanguageMode mode, string fileName)
{
    public (IRTree, TypeSystem, SymbolTable, IGlykonError[]) Analyze()
    {
        TypeSystem typeSystem = new(interner);
        typeSystem.BuildPrimitives();

        SemanticBinder binder = new(syntaxTree, typeSystem, interner, mode, fileName);

        var (boundTree, st, binderErrors) = binder.Bind();

        FlowAnalyzer flowAnalyzer = new(boundTree, fileName);
        var flowErrors = flowAnalyzer.Analyze();
        
        IRTypeBuilder irTypeBuilder = new(boundTree, typeSystem, interner, fileName);
        var (irTree, typeErrors) = irTypeBuilder.Build();
        
        ConstantFolder folder = new(irTree, typeSystem, interner, fileName);
        var (foldedTree, foldingErrors) = folder.Fold();
        
        return (foldedTree, typeSystem, st, [.. binderErrors, ..flowErrors, ..typeErrors, ..foldingErrors]);
    }
}
