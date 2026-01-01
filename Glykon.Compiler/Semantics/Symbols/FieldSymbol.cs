using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class FieldSymbol(int nameId, TypeSymbol parentType, TypeSymbol type) : Symbol(nameId, type)
{
    private TypeSymbol ParentType { get; } = parentType;
    
    public override bool Equals(object? obj)
    {
        if (obj is not FieldSymbol other) return false;
        return other.NameId == NameId && other.ParentType == ParentType;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(NameId, ParentType.SerialId);
    }
}