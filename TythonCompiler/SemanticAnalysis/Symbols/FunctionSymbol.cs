using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis.Symbols
{
    public class FunctionSymbol(TokenType returnType, TokenType[] parameterTypes)
    {
        public TokenType ReturnType { get; } = returnType;
        public TokenType[] ParameterTypes { get; } = parameterTypes;
    }
}
