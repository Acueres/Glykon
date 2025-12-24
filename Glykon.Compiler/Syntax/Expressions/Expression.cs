namespace Glykon.Compiler.Syntax.Expressions;

public enum ExpressionKind : byte
{
    Unary,
    Binary,
    Call,
    Grouping,
    Literal,
    Name,
    Assignment,
    MemberAccess,
    Logical,
    Conversion,
    Range
}

public abstract class Expression
{
    public abstract ExpressionKind Kind { get; }
}
