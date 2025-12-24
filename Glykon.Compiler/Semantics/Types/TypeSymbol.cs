using Glykon.Compiler.Semantics.Symbols;

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

public class TypeSymbol(
    int serialId,
    int nameId,
    TypeKind kind)
{
    public int NameId { get; } = nameId;
    public TypeKind Kind { get; } = kind;
    public bool IsPrimitive => Kind is TypeKind.Int64 or TypeKind.Float64 or TypeKind.Bool or TypeKind.String;
    public bool IsNumeric => Kind is TypeKind.Int64 or TypeKind.Float64;
    public bool IsNone => Kind == TypeKind.None;
    public bool IsError => Kind == TypeKind.Error;

    public MethodSymbol[] Methods { get; set; } = [];
    public FieldSymbol[] Fields { get; set; } = [];
    public ConstantSymbol[] Constants { get; set; } = [];
    public TypeSymbol[] NestedTypes { get; set; } = [];

    private int SerialId { get; } = serialId;

    public void FinalizeType(MethodSymbol[] methods,
        FieldSymbol[] fields,
        ConstantSymbol[] constants,
        TypeSymbol[] nestedTypes)
    {
        Methods = methods;
        Fields = fields;
        Constants = constants;
        NestedTypes = nestedTypes;
    }

    public Symbol? Find(int nameId)
    {
        var field = Fields.FirstOrDefault(f => f.NameId == nameId);
        if (field is not null) return field;
        
        var constant = Constants.FirstOrDefault(c => c.NameId == nameId);
        if (constant is not null) return constant;
        
        var method = Methods.FirstOrDefault(m => m.NameId == nameId);
        return method;
    }

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
