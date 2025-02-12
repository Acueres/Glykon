namespace Tython.Model
{
    public class VariableSymbol(TokenType type)
    {
        public int LocalIndex { get; set; }
        public TokenType Type { get; } = type;
    }

    public class ParameterSymbol(int index, TokenType type)
    {
        public int Index { get; } = index;
        public TokenType Type { get; } = type;
    }

    public class ConstantSymbol(object value, TokenType type)
    {
        public object Value { get; } = value;
        public TokenType Type { get; } = type;
    }

    public class FunctionSymbol(TokenType returnType, TokenType[] parameterTypes)
    {
        public TokenType ReturnType { get; } = returnType;
        public TokenType[] ParameterTypes { get; } = parameterTypes;
    }
}
