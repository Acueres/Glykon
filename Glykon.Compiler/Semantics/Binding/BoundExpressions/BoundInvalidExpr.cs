namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundInvalidExpr : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Invalid;
}