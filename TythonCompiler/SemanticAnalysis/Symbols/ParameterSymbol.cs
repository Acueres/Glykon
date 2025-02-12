using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis.Symbols
{
    public class ParameterSymbol(int index, TokenType type)
    {
        public int Index { get; } = index;
        public TokenType Type { get; } = type;
    }
}
