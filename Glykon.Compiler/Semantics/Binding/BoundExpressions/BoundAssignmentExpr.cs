using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundAssignmentExpr(BoundExpression target, BoundExpression value) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Assignment;

    public BoundExpression Target { get; } = target;
    public BoundExpression Value { get; } = value;
}
