namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundRangeExpr(BoundExpression start, BoundExpression end, BoundExpression? step, bool isInclusive)  : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Range;

    public BoundExpression Start { get; } = start;
    public BoundExpression End { get; } = end;
    public BoundExpression? Step { get; } = step;
    public bool IsInclusive { get; }  = isInclusive;
}