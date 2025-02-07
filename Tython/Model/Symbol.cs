namespace Tython.Model
{
    public class VariableSymbol(int index, TokenType type)
    {
        public int LocalIndex { get; } = index;
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
