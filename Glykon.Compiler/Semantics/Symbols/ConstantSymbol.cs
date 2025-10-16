using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class ConstantSymbol(int nameId, TokenKind type, Token value) : Symbol(nameId, type)
{
    public Token Value { get; } = value;
}

