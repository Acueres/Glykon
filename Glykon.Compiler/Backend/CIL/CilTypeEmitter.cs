using System.Reflection;
using System.Reflection.Emit;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;

namespace Glykon.Compiler.Backend.CIL;

public class CilTypeEmitter(IRClassDeclaration classDeclaration, IdentifierInterner interner)
{
    public void EmitType(ModuleBuilder mob)
    {
        TypeBuilder tb = mob.DefineType(interner[classDeclaration.Type.NameId],
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        var nested = classDeclaration.Nested.Select(n => new CilTypeEmitter((IRClassDeclaration)n, interner)).ToArray();

        foreach (var type in nested)
        {
            type.EmitType(tb);
        }
        
        var fields = classDeclaration.Fields.Select(f => EmitField(f, tb)).ToArray();
    }
    
    private void EmitType(TypeBuilder parentType)
    {
        TypeBuilder tb = parentType.DefineNestedType(interner[classDeclaration.Type.NameId],
            TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed);

        var nested = classDeclaration.Nested.Select(n => new CilTypeEmitter((IRClassDeclaration)n, interner)).ToArray();

        foreach (var type in nested)
        {
            type.EmitType(tb);
        }
        
        var fields = classDeclaration.Fields.Select(f => EmitField(f, tb)).ToArray();
    }

    private FieldInfo EmitField(IRFieldDeclaration field, TypeBuilder tb)
    {
        string fieldName = interner[field.Symbol.NameId];
        var type = IntrinsicClrTypeTranslator.Translate(field.Symbol.Type);
        var fieldInfo = tb.DefineField(fieldName, type, FieldAttributes.Private);
        return fieldInfo;
    }
}