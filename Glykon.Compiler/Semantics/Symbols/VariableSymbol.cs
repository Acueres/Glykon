using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class VariableSymbol(int id, TokenKind type) : Symbol(id, type)
{
    public int LocalIndex { get; set; }
}
