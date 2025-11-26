using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Analysis;

public sealed record SemanticResult(
    SyntaxTree SyntaxTree,
    Token[] Tokens,
    IRTree Ir,
    TypeSystem TypeSystem,
    IdentifierInterner Interner,
    SymbolTable SymbolTable,
    IGlykonError[] LexErrors,
    IGlykonError[] ParseErrors,
    IGlykonError[] SemanticErrors)
{
    public IEnumerable<IGlykonError> AllErrors =>
        LexErrors.Concat(ParseErrors).Concat(SemanticErrors);
}