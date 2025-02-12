using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis.Symbols
{
    public class ConstantSymbol(object value, TokenType type)
    {
        public object Value { get; } = value;
        public TokenType Type { get; } = type;
    }
}
