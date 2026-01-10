using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public static class ClrIntrinsicTypeTranslator
{
    public static Type Translate(TypeSymbol type)
    {
        if (TryTranslate(type, out var t))
        {
            return t!;
        }

        throw new NotSupportedException(
            $"Type '{type}' (kind: {type.Kind}) is not supported for value position.");
    }

    private static bool TryTranslate(TypeSymbol type, out Type? result)
    {
        result = type.Kind switch
        {
            TypeKind.Bool    => typeof(bool),
            TypeKind.Int64   => typeof(long),
            TypeKind.Float64 => typeof(double),
            TypeKind.String  => typeof(string),
            TypeKind.None    => typeof(void),
            _ => null
        };

        return result is not null;
    }
}