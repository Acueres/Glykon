using Glykon.Compiler.Core;

namespace Glykon.Compiler.Syntax;

public enum TokenKind : byte
{
    // Sentinels
    Empty, VirtualTerminator,
    // Cursor (for parser prediction mode)
    Cursor,
    Newline,

    // Literals
    None, LiteralInt, LiteralReal, LiteralString, LiteralMultilineString, LiteralTrue, LiteralFalse,

    // Symbols
    BracketLeft, BracketRight, //[]
    ParenthesisLeft, ParenthesisRight, //()
    BraceLeft, BraceRight, //{}
    
    //, . - + ; : / // * ** = -> .. ..=
    Comma, Dot, Minus, Plus, Semicolon, Colon, Slash,
    SlashDouble, Star, StarDouble, Assignment, Arrow, Range, RangeInclusive,

    NotEqual, Equal, //!= ==
    Greater, GreaterEqual, //> >=
    Less, LessEqual, //< <=

    // Keywords
    Identifier, Class, Struct, Interface, New, Enum, Def, Let, Const,
    And, Not, Or,
    If, Else, Elif, For, By, In, As, While, Return, Break, Continue,

    EOF
}

public readonly struct Token
{
    public TokenKind Kind { get; }
    public int Line { get; }
    public TextSpan? Span { get; }

    public string Text => Span!.Value.Text;

    private static readonly Token empty = new(TokenKind.Empty, 0);
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
