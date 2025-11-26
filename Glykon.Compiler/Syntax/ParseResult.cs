using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Compiler.Syntax;

public sealed record ParseResult(
    SyntaxTree SyntaxTree,
    Token[] Tokens,
    IGlykonError[] LexErrors,
    IGlykonError[] ParseErrors)
{
    public IEnumerable<IGlykonError> AllErrors => LexErrors.Concat(ParseErrors);
}