using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundGroupingExpr(BoundExpression expr) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Grouping;
    public BoundExpression Expression { get; } = expr;
}
