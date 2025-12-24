namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public enum BoundExpressionKind : byte
{
    Invalid,
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

public abstract class BoundExpression
{
    public abstract BoundExpressionKind Kind { get; }
}