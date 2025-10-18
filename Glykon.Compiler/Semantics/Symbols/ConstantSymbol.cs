using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class ConstantSymbol(int nameId, TokenKind type, ConstantValue value) : Symbol(nameId, type)
{
    public ConstantValue Value { get; } = value;
}

