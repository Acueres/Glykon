using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public abstract class Symbol(int id, TokenKind type)
{
    public int Id { get; } = id;
    public TokenKind Type { get; set; } = type;
}