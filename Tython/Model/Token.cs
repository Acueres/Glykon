using Tython.Enum;

namespace Tython.Model
{
    public readonly struct Token
    {
        public readonly object? Value { get; }
        public readonly int Line { get; }
        public readonly TokenType Type { get; }
        public bool IsNull => Type == TokenType.Null;

        public static Token Null => new();

        public Token(object value, int line, TokenType type)
        {
            Value = value;
            Line = line;
            Type = type;
        }

        public Token(TokenType type, int line)
        {
            Type = type;
            Line = line;

            if (Type == TokenType.True)
            {
                Value = true;
            }
            else if (Type == TokenType.False)
            {
                Value = false;
            }
        }
    }
}
