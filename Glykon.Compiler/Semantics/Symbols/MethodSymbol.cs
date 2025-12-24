using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class MethodSymbol(int nameId, TypeSymbol returnType, TypeSymbol parentType, TypeSymbol[] parameters, bool isStatic) : Symbol(nameId, returnType)
{
    public TypeSymbol ParentType { get; } = parentType;
    public TypeSymbol[] Parameters { get; } = parameters;
    public bool IsStatic { get; } = isStatic;
}