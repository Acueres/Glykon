using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class FieldSymbol(int nameId, TypeSymbol type, TypeSymbol parentType) : Symbol(nameId, type)
{
    public TypeSymbol ParentType { get; } = parentType;
    public int FieldIndex { get; set; }
}