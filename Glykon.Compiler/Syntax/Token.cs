namespace Glykon.Compiler.Syntax;

public enum TokenKind : byte
{
    // Sentinel
    Empty,

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

public readonly struct Token
{
    public TokenKind Kind { get; init; }
    public int Line { get; init; }

    public string StringValue { get; init; } = string.Empty;
    public long IntValue { get; init; }
    public double RealValue { get; init; }

    static readonly Token empty = new(TokenKind.Empty, 0);
    public static ref readonly Token Empty => ref empty;

    public bool IsEmpty => Kind == TokenKind.Empty;

    public Token(TokenKind type, int line, string value)
    {
        Kind = type;
        Line = line;
        StringValue = value;
    }

    public Token(TokenKind type, int line, long value)
    {
        Kind = type;
        Line = line;
        IntValue = value;
    }

    public Token(TokenKind type, int line, double value)
    {
        Kind = type;
        Line = line;
        RealValue = value;
    }

    public Token(TokenKind type, int line)
    {
        Kind = type;
        Line = line;
    }
}
