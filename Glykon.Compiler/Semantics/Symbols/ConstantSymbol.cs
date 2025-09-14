using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class ConstantSymbol(int id, TokenKind type, Token value) : Symbol(id, type)
{
    public Token Value { get; } = value;
}

