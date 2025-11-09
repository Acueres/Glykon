using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public static class IntrinsicClrTypeTranslator
{
    public static Type Translate(TypeSymbol type)
    {
        if (TryTranslate(type, out var t))
            return t;

        throw new NotSupportedException(
            $"Type '{type}' (kind: {type.Kind}) is not supported for value position.");
    }

    static bool TryTranslate(TypeSymbol type, out Type result)
    {
        result = type.Kind switch
        {
            TypeKind.Bool    => typeof(bool),
            TypeKind.Int64   => typeof(long),
            TypeKind.Float64 => typeof(double),
            TypeKind.String  => typeof(string),
            _ => typeof(void)
        };

        return true;
    }
    
    public static Type[] Translate(ReadOnlySpan<TypeSymbol> types)
    {
        var arr = new Type[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            arr[i] = Translate(types[i]);
        }

        return arr;
    }
}