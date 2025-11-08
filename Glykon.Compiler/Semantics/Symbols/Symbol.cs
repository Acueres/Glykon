using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public abstract class Symbol(int nameId, TypeSymbol type)
{
    public int NameId { get; } = nameId;
    public TypeSymbol Type { get; internal set; } = type;

    public override bool Equals(object? obj)
    {
        if (obj is not Symbol other) return false;
        return other.NameId == NameId;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NameId, Type.NameId);
    }
}