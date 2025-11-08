using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class ConstantSymbol(int nameId, TypeSymbol type) : Symbol(nameId, type)
{
    public ConstantValue Value { get; set; }
}

