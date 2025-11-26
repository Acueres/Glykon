namespace Glykon.Compiler.Syntax.Expressions;

public class RangeExpr(Expression start, Expression end, Expression? step, bool isInclusive) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Range;

    public Expression Start { get; } = start;
    public Expression End { get; } = end;
    public Expression? Step { get; } = step;
    public bool IsInclusive { get; }  = isInclusive;
}