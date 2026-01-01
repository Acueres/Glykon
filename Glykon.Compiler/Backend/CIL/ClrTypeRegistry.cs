using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public sealed class ClrTypeRegistry
{
    private readonly Dictionary<TypeSymbol, Type> map = [];

    public void Register(TypeSymbol glykonType, Type clrType)
        => map[glykonType] = clrType;

    public Type Resolve(TypeSymbol type)
    {
        if (type.Kind != TypeKind.Defined)
        {
            return ClrIntrinsicTypeTranslator.Translate(type);
        }

        if (map.TryGetValue(type, out var t))
        {
            return t;
        }

        throw new NotSupportedException($"Unresolved CLR type for '{type}' (kind: {type.Kind}).");
    }
}