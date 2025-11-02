using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundLogicalExpr(Token oper, BoundExpression left, BoundExpression right)
    : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Logical;
    public Token Operator { get; } = oper;
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;
}
