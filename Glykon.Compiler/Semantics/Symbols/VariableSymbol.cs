using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class VariableSymbol(int nameId, TypeSymbol type) : Symbol(nameId, type)
{
    public int LocalIndex { get; set; }

    public void UpdateType(TypeSymbol type)
    {
        Type = type;
    }
}
