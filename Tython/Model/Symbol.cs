namespace Tython.Model
{
    public class VariableSymbol(int index, int symbolId, TokenType type)
    {
        public int LocalIndex { get; } = index;
        public int SymbolId { get; } = symbolId;
        public TokenType Type { get; } = type;
    }

    public class FunctionSymbol(int symbolId, TokenType returnType, TokenType[] parameterTypes)
    {
        public int SymbolId { get; } = symbolId;
        public TokenType ReturnType { get; } = returnType;
        public TokenType[] ParameterTypes { get; } = parameterTypes;
    }
}
