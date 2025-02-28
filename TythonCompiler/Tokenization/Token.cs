namespace TythonCompiler.Tokenization
{
    public enum TokenType : byte
    {
        Null,
        //literals
        Identifier, Int, Real, True, False, String, Bool, None,

        //symbols
        BracketLeft, BracketRight, //[]
        ParenthesisLeft, ParenthesisRight, //()
        BraceLeft, BraceRight, //{}

        Comma, Dot, Minus, Plus, Semicolon, Colon, Slash, SlashDouble, Star, StarDouble, Assignment, Arrow, //, . - + ; : / // * ** = ->

        NotEqual, Equal, //!= ==
        Greater, GreaterEqual, //> >=
        Less, LessEqual, //< <=

        //keywords
        Class, Struct, Interface, Enum, Def, Let, Const,
        And, Not, Or,
        If, Else, Elif, For, While, Return, Break, Continue,

        EOF
    }

    public readonly struct Token
    {
        public readonly object Value { get; }
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
