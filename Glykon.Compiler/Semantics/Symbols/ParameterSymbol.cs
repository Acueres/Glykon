using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols
{
    public class ParameterSymbol(int nameId, TokenKind type, int index) : Symbol(nameId, type)
    {
        public int Index { get; } = index;
    }
}
