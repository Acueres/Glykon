using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;

namespace Glykon.Compiler.Semantics.Types;

public class TypeSystem(IdentifierInterner interner)
{
    readonly IdentifierInterner interner = interner;
    readonly Dictionary<int, TypeSymbol> types = [];

    int typeSerial = (int)TypeKind.SerialStart;

    public void BuildPrimitives()
    {
        int noneId = interner.Intern("none");
        types.Add(noneId, new TypeSymbol((int)TypeKind.None, noneId, TypeKind.None));

        int integerId = interner.Intern("int");
        types.Add(integerId, new TypeSymbol((int)TypeKind.Int64, integerId, TypeKind.Int64));

        int realId = interner.Intern("real");
        types.Add(realId, new TypeSymbol((int)TypeKind.Float64, realId, TypeKind.Float64));

        int boolId = interner.Intern("bool");
        types.Add(boolId, new TypeSymbol((int)TypeKind.Bool, boolId, TypeKind.Bool));

        int stringId = interner.Intern("str");
        types.Add(stringId, new TypeSymbol((int)TypeKind.String, stringId, TypeKind.String));
        
        int errorId = interner.Intern("_error");
        types.Add(errorId, new TypeSymbol((int)TypeKind.Error, stringId, TypeKind.Error));
    }

    // TODO: Add more numeric types
    public TypeSymbol GetCommonNumericType(TypeSymbol a, TypeSymbol b)
    {
        if (a == b) return a;
        
        bool isNumA = a.Kind is TypeKind.Int64 or TypeKind.Float64;
        bool isNumB = b.Kind is TypeKind.Int64 or TypeKind.Float64;
        if (!(isNumA && isNumB))
            return this[TypeKind.Error];

        // Promotion lattice (widening)
        if (a.Kind == TypeKind.Float64 || b.Kind == TypeKind.Float64)
            return this[TypeKind.Float64];
        
        return this[TypeKind.Int64];
    }

    public static bool CanImplicitlyConvert(TypeSymbol s1, TypeSymbol s2) => s1 != s2 && s1.IsNumeric && s2.IsNumeric;

    public IEnumerable<TypeSymbol> GetPrimitives()
    {
        yield return this[TypeKind.None];
        yield return this[TypeKind.Int64];
        yield return this[TypeKind.Float64];
        yield return this[TypeKind.Bool];
        yield return this[TypeKind.String];
    }

    public TypeSymbol this[TypeKind kind] =>
        // Cast is guaranteed to always be correct when the primitives are registered first
        types[(int)kind];

    public TypeSymbol this[ConstantKind kind]
    {
        get
        {
            var typeKind = kind switch
            {
                ConstantKind.Int => TypeKind.Int64,
                ConstantKind.Real => TypeKind.Float64,
                ConstantKind.String => TypeKind.String,
                ConstantKind.Bool => TypeKind.Bool,
                _ => TypeKind.None,
            };

            return this[typeKind];
        }
    }
}
