namespace TythonCompiler.Tokenization;

public enum TokenType
{
    // Literals
    None, LiteralInt, LiteralReal, LiteralString, LiteralTrue, LiteralFalse,

    // Literal types
    Int, Real, String, Bool,

    // Symbols
    BracketLeft, BracketRight, //[]
    ParenthesisLeft, ParenthesisRight, //()
    BraceLeft, BraceRight, //{}

    Comma, Dot, Minus, Plus, Semicolon, Colon, Slash, SlashDouble, Star, StarDouble, Assignment, Arrow, //, . - + ; : / // * ** = ->

    NotEqual, Equal, //!= ==
    Greater, GreaterEqual, //> >=
    Less, LessEqual, //< <=

    // Keywords
    Identifier, Class, Struct, Interface, Enum, Def, Let, Const,
    And, Not, Or,
    If, Else, Elif, For, While, Return, Break, Continue,

    EOF
}

public class Token
{
    public object Value { get; init; }
    public int Line { get; init; }
    public TokenType Type { get; init; }

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

        if (Type == TokenType.LiteralTrue)
        {
            Value = true;
        }
        else
        {
            Value = false;
        }
    }
}
