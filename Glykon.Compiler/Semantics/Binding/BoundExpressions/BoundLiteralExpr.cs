using Glykon.Compiler.Core;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundLiteralExpr(ConstantValue value) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Literal;
    public ConstantValue Value { get; } = value;
}
