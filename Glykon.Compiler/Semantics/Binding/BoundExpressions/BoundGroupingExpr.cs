namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundGroupingExpr(BoundExpression expr) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Grouping;
    public BoundExpression Expression { get; } = expr;
}
