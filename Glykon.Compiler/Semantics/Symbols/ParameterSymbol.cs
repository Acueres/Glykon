using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols
{
    public class ParameterSymbol(int id, TokenType type, int index) : Symbol(id, type)
    {
        public int Index { get; } = index;
    }
}
