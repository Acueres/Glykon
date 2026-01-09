using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public class CilTypeEmitter
{
    private readonly TypeBuilder tb;

    private readonly IdentifierInterner interner;
    private readonly ClrTypeRegistry typeRegistry;

    private readonly TypeSymbol type;
    private readonly IRTypeDeclaration[] nested;
    private readonly IRFieldDeclaration[] fields;
    private readonly IRMethodDeclaration[] methods;

    private readonly List<CilTypeEmitter> nestedEmitters = [];

    public CilTypeEmitter(IRTypeDeclaration typeDeclaration, ModuleBuilder mob, IdentifierInterner interner,
        ClrTypeRegistry typeRegistry) : this(
        interner, typeRegistry, typeDeclaration.Type, typeDeclaration.Nested, typeDeclaration.Fields,
        typeDeclaration.Methods)
    {
        var attrs = GetAttributes();
        tb = mob.DefineType(interner[typeDeclaration.Type.NameId], attrs,
            type.IsValueType ? typeof(ValueType) : typeof(object));
    }

    private CilTypeEmitter(IRTypeDeclaration typeDeclaration, TypeBuilder parentType, IdentifierInterner interner,
        ClrTypeRegistry typeRegistry) :
        this(interner, typeRegistry, typeDeclaration.Type, typeDeclaration.Nested, typeDeclaration.Fields,
            typeDeclaration.Methods)
    {
        var attrs = GetAttributes(isNested: true);
        tb = parentType.DefineNestedType(interner[typeDeclaration.Type.NameId], attrs,
            type.IsValueType ? typeof(ValueType) : typeof(object));
    }

    private CilTypeEmitter(IdentifierInterner interner, ClrTypeRegistry typeRegistry, TypeSymbol type,
        IRTypeDeclaration[] nested, IRFieldDeclaration[] fields, IRMethodDeclaration[] methods)
    {
        this.interner = interner;
        this.typeRegistry = typeRegistry;
        this.type = type;
        this.nested = nested;
        this.fields = fields;
        this.methods = methods;
    }

    public void CreateType()
    {
        foreach (var nestedEmitter in nestedEmitters)
        {
            nestedEmitter.CreateType();
        }

        tb.CreateType();
    }

    public void DefineType()
    {
        typeRegistry.Register(type, tb);
        DefineNested();
    }

    private TypeAttributes GetAttributes(bool isNested = false)
    {
        var attrs = isNested ? TypeAttributes.NestedPublic : TypeAttributes.Public;
        attrs |= TypeAttributes.Sealed;

        if (type.IsValueType)
        {
            return attrs | TypeAttributes.SequentialLayout;
        }

        return attrs | TypeAttributes.Class;
    }

    private void DefineNested()
    {
        foreach (var nestedDecl in nested)
        {
            var emitter = new CilTypeEmitter(nestedDecl, tb, interner, typeRegistry);
            nestedEmitters.Add(emitter);
            emitter.DefineType();
        }
    }

    public (MethodSymbol, CilCallableEmitter)[] DefineMethods()
    {
        List<(MethodSymbol, CilCallableEmitter)> callableEmitters = [];
        foreach (var nestedTypeMethods in nestedEmitters
                     .Select(nestedDecl => nestedDecl.DefineMethods()))
        {
            callableEmitters.AddRange(nestedTypeMethods);
        }

        callableEmitters.AddRange(methods.Select(method =>
            (method.Signature, new CilCallableEmitter(method, typeRegistry, interner, tb))));

        return callableEmitters.ToArray();
    }

    public IEnumerable<(TypeSymbol, ConstructorBuilder)> DefineConstructors()
    {
        if (!type.IsValueType)
        {
            var ctor = EmitDefaultCtor(tb);
            yield return (type, ctor);
        }

        foreach (var nestedEmitter in nestedEmitters)
        {
            var nestedConstructors = nestedEmitter.DefineConstructors();
            foreach (var nestedConstructor in nestedConstructors)
            {
                yield return nestedConstructor;
            }
        }
    }

    private static ConstructorBuilder EmitDefaultCtor(TypeBuilder tb)
    {
        var attrs =
            MethodAttributes.Public |
            MethodAttributes.HideBySig |
            MethodAttributes.SpecialName |
            MethodAttributes.RTSpecialName;

        var ctor = tb.DefineConstructor(attrs, CallingConventions.Standard, Type.EmptyTypes);

        var il = ctor.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Call, typeof(object).GetConstructor(Type.EmptyTypes)!);
        il.Emit(OpCodes.Ret);

        return ctor;
    }

    public (FieldSymbol, FieldInfo)[] EmitFields()
    {
        List<(FieldSymbol, FieldInfo)> fieldData = [];
        foreach (var field in fields)
        {
            string fieldName = interner[field.Symbol.NameId];
            var fieldType = typeRegistry.Resolve(field.Symbol.Type);
            var fieldInfo = tb.DefineField(fieldName, fieldType, FieldAttributes.Public);
            fieldData.Add((field.Symbol, fieldInfo));
        }

        foreach (var nestedEmitter in nestedEmitters)
        {
            fieldData.AddRange(nestedEmitter.EmitFields());
        }

        return fieldData.ToArray();
    }
}