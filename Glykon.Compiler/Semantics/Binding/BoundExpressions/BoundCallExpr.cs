namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundCallExpr(BoundExpression callee, BoundExpression[] parameters) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Call;
    public BoundExpression Callee { get; } = callee;
    public BoundExpression[] Parameters { get; } = parameters;
}