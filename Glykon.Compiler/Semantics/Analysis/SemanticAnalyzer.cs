using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Flow;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.Lowering;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Analysis;

public class SemanticAnalyzer(ParseResult parseResult, LanguageMode mode, string fileName)
{
    public SemanticResult Analyze()
    {
        IdentifierInterner interner = new();
        
        TypeSystem typeSystem = new(interner);
        typeSystem.BuildPrimitives();

        SemanticBinder binder = new(parseResult.SyntaxTree, typeSystem, interner, mode, fileName);

        var (boundTree, st, binderErrors) = binder.Bind();

        FlowAnalyzer flowAnalyzer = new(boundTree, fileName);
        var flowErrors = flowAnalyzer.Analyze();

        IRTypeBuilder irTypeBuilder = new(boundTree, typeSystem, interner, fileName);
        var (irTree, typeErrors) = irTypeBuilder.Build();

        ConstantFolder folder = new(irTree, typeSystem, interner, fileName);
        var (foldedTree, foldingErrors) = folder.Fold();
        
        Lowerer lowerer = new(foldedTree, interner, typeSystem, st, mode);
        var loweredTree = lowerer.Lower();

        return new SemanticResult(parseResult.SyntaxTree, parseResult.Tokens, loweredTree, typeSystem, interner, st,
            parseResult.LexErrors, parseResult.ParseErrors,
            [.. binderErrors, ..flowErrors, ..typeErrors, ..foldingErrors]);
    }
}
