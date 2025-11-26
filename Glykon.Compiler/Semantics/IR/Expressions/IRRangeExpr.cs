namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRRangeExpr(IRExpression start, IRExpression end, IRExpression? step, bool isInclusive)
    : IRExpression(start.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Range;

    public IRExpression Start { get; } = start;
    public IRExpression End { get; } = end;
    public IRExpression? Step { get; } = step;
    public bool IsInclusive { get; } = isInclusive;
}