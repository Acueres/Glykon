using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class VariableSymbol(int nameId, bool immutable, TypeSymbol type) : Symbol(nameId, type)
{
    public int LocalIndex { get; set; }
    public bool Immutable { get; } = immutable;

    public void UpdateType(TypeSymbol type)
    {
        Type = type;
    }
}
