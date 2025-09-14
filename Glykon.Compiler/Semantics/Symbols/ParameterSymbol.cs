using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols
{
    public class ParameterSymbol(int id, TokenKind type, int index) : Symbol(id, type)
    {
        public int Index { get; } = index;
    }
}
