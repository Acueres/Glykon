using Glykon.Compiler.Core;

namespace Glykon.Compiler.Syntax;

public enum TokenKind : byte
{
    // Sentinel
    Empty,

    // Literals
    None, LiteralInt, LiteralReal, LiteralString, LiteralTrue, LiteralFalse,

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
    public TextSpan? Span { get; }

    public string Text => Span!.Value.Text;

    static readonly Token empty = new(TokenKind.Empty, 0);
    public static ref readonly Token Empty => ref empty;

    public bool IsEmpty => Kind == TokenKind.Empty;

    public Token(TokenKind type, int line)
    {
        Kind = type;
        Line = line;
    }

    public Token(TokenKind type, int line, TextSpan span)
    {
        Kind = type;
        Line = line;
        Span = span;
    }
}
