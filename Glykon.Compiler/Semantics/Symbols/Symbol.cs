using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public abstract class Symbol(int nameId, TokenKind type)
{
    public int NameId { get; } = nameId;
    public TokenKind Type { get; set; } = type;

    public override bool Equals(object? obj)
    {
        if (obj == null || obj is not Symbol other) return false;
        return other.NameId == NameId;
    }

    public override int GetHashCode()
    {
        return NameId.GetHashCode();
    }
}