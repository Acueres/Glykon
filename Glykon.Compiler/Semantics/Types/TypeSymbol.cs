namespace Glykon.Compiler.Semantics.Types;

public enum TypeKind
{
    None,
    Int64,
    Float64,
    Bool,
    String,
    Defined,
    SerialStart
}

public class TypeSymbol(int serialId, int nameId, TypeKind kind)
{
    public int SerialId { get; } = serialId;
    public int NameId { get; } = nameId;
    public TypeKind Kind { get; } = kind;

    public override bool Equals(object? obj)
    {
        if (obj is not TypeSymbol sb) return false;
        return SerialId == sb.SerialId;
    }

    public override int GetHashCode()
    {
        return SerialId.GetHashCode();
    }
}
