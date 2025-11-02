namespace Glykon.Compiler.Semantics.Types;

public enum TypeKind
{
    None,
    Int64,
    Float64,
    Bool,
    String,
    Error,
    Defined,
    SerialStart
}

public class TypeSymbol(int serialId, int nameId, TypeKind kind)
{
    public int SerialId { get; } = serialId;
    public int NameId { get; } = nameId;
    public TypeKind Kind { get; } = kind;
    public bool IsPrimitive => Kind is TypeKind.Int64 or TypeKind.Float64 or TypeKind.Bool or TypeKind.String;
    public bool IsNumeric => Kind is TypeKind.Int64 or TypeKind.Float64;
    public bool IsNone => Kind == TypeKind.None;
    public bool IsError => Kind == TypeKind.Error;

    public static bool operator ==(TypeSymbol a, TypeSymbol b)
    {
        return a.SerialId == b.SerialId;
    }
    
    public static bool operator !=(TypeSymbol a, TypeSymbol b)
    {
        return a.SerialId != b.SerialId;
    }

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
