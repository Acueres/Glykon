namespace Tython.Model
{
    public readonly struct Token(string lexeme, int line, TokenType type)
    {
        public readonly string Lexeme { get; } = lexeme;
        public readonly int Line { get; } = line;
        public readonly TokenType Type { get; } = type;
        public bool IsNull => Type == TokenType.Null;

        public static Token Null => new();
    }
}
