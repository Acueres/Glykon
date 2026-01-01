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
    private readonly IRClassDeclaration[] nested;
    private readonly IRFieldDeclaration[] fields;
    private readonly IRMethodDeclaration[] methods;
    
    private readonly List<CilTypeEmitter> nestedEmitters = [];
    
    public CilTypeEmitter(IRClassDeclaration classDeclaration, ModuleBuilder mob, IdentifierInterner interner, ClrTypeRegistry typeRegistry) : this(
        interner, typeRegistry, classDeclaration.Type, classDeclaration.Nested, classDeclaration.Fields, classDeclaration.Methods)
    {
        tb = mob.DefineType(interner[classDeclaration.Type.NameId],
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);
    }

    public CilTypeEmitter(IRClassDeclaration classDeclaration, TypeBuilder parentType, IdentifierInterner interner, ClrTypeRegistry typeRegistry) :
        this(interner, typeRegistry, classDeclaration.Type, classDeclaration.Nested, classDeclaration.Fields, classDeclaration.Methods)
    {
        tb = parentType.DefineNestedType(interner[classDeclaration.Type.NameId],
            TypeAttributes.Class | TypeAttributes.NestedPublic | TypeAttributes.Sealed);
    }

    private CilTypeEmitter(IdentifierInterner interner, ClrTypeRegistry typeRegistry, TypeSymbol type,
        IRClassDeclaration[] nested, IRFieldDeclaration[] fields, IRMethodDeclaration[] methods)
    {
        this.interner = interner;
        this.typeRegistry = typeRegistry;
        this.type = type;
        this.nested = nested;
        this.fields = fields;
        this.methods = methods;
    }

    private TypeBuilder GetTypeBuilder() => tb;

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
    
    private void DefineNested()
    {
        foreach (var nestedDecl in nested)
        {
            var emitter = new CilTypeEmitter(nestedDecl, tb, interner, typeRegistry);
            nestedEmitters.Add(emitter);
            emitter.DefineType();
        }
    }

    public (CilCallableEmitter, MethodSymbol)[] DefineMethods()
    {
        List<(CilCallableEmitter, MethodSymbol)> callableEmitters = [];
        foreach (var nestedTypeMethods in nestedEmitters
                     .Select(nestedDecl => nestedDecl.DefineMethods()))
        {
            callableEmitters.AddRange(nestedTypeMethods);
        }

        callableEmitters.AddRange(methods.Select(method =>
            (new CilCallableEmitter(method, typeRegistry, interner, tb), method.Signature)));

        return callableEmitters.ToArray();
    }

    public (FieldInfo, FieldSymbol)[] EmitFields()
    {
        List<(FieldInfo, FieldSymbol)> fieldData = [];
        foreach (var field in fields)
        {
            string fieldName = interner[field.Symbol.NameId];
            var fieldType = typeRegistry.Resolve(field.Symbol.Type);
            var fieldInfo = tb.DefineField(fieldName, fieldType, FieldAttributes.Public);
            fieldData.Add((fieldInfo, field.Symbol));
        }

        foreach (var nestedEmitter in nestedEmitters)
        {
            fieldData.AddRange(nestedEmitter.EmitFields());
        }
        
        return fieldData.ToArray();
    }
}