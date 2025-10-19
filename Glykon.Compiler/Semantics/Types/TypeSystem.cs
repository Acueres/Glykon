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
    }

    public IEnumerable<TypeSymbol> GetPrimitives()
    {
        yield return this[TypeKind.None];
        yield return this[TypeKind.Int64];
        yield return this[TypeKind.Float64];
        yield return this[TypeKind.Bool];
        yield return this[TypeKind.String];
    }

    public TypeSymbol this[TypeKind kind]
    {
        // Cast is guaranteed to always be correct when the primitives are registered first
        get => types[(int)kind];
    }
}
