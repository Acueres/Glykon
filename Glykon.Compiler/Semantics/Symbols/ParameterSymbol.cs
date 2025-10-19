using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class ParameterSymbol(int nameId, TypeSymbol type, int index) : Symbol(nameId, type)
{
    public int Index { get; } = index;
}
