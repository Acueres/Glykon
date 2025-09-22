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
    public TokenKind Kind { get; }
    public int Line { get; }
    
    public bool IsBool => Kind is TokenKind.Bool or TokenKind.LiteralTrue or TokenKind.LiteralFalse;
    public bool BoolValue => Kind == TokenKind.LiteralTrue;
    public string StringValue { get; } = string.Empty;
    public long IntValue { get; }
    public double RealValue { get; }

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
