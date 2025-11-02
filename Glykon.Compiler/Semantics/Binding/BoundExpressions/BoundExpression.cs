namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public enum BoundExpressionKind : byte
{
    Invalid,
    Unary,
    Binary,
    Call,
    Grouping,
    Literal,
    Variable,
    Assignment,
    Logical,
    Conversion
}

public abstract class BoundExpression
{
    public abstract BoundExpressionKind Kind { get; }
}