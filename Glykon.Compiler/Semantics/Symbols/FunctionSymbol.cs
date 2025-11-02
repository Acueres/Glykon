using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class FunctionSymbol(int nameId, int serialId, int qualifiedId, TypeSymbol returnType, TypeSymbol[] parameters) : Symbol(nameId, returnType)
{
    private int SerialId { get; } = serialId;
    public int QualifiedNameId { get; } = qualifiedId;
    public TypeSymbol[] Parameters { get; } = parameters;

    public Scope? Scope { get; set; }

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionSymbol sb) return false;
        return SerialId == sb.SerialId;
    }

    public override int GetHashCode()
    {
        return SerialId.GetHashCode();
    }
}

