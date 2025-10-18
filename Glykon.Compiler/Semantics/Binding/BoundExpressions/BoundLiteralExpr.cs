using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundLiteralExpr(ConstantValue value) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Literal;
    public ConstantValue Value { get; } = value;
}
