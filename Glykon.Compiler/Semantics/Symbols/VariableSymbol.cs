using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class VariableSymbol(int nameId, TokenKind type) : Symbol(nameId, type)
{
    public int LocalIndex { get; set; }
}
