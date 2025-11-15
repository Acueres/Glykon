using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Compiler.Syntax;

public sealed record LexResult(
    Token[] Tokens,
    IGlykonError[] Errors);