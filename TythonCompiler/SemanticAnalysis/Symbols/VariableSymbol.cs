using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis.Symbols
{
    public class VariableSymbol(TokenType type)
    {
        public int LocalIndex { get; set; }
        public TokenType Type { get; } = type;
    }
}
