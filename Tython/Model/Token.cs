using Tython.Enum;

namespace Tython.Model
{
    public readonly struct Token
    {
        public readonly string Value { get; }
        public readonly int Line { get; }
        public readonly TokenType Type { get; }
        public bool IsNull => Type == TokenType.Null;

        public static Token Null => new();

        public Token(string value, int line, TokenType type)
        {
            Value = value;
            Line = line;
            Type = type;
        }

        public Token(TokenType type, int line)
        {
            Type = type;
            Line = line;
            Value = string.Empty;
        }
    }
}
