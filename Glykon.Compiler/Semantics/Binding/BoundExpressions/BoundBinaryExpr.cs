using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundBinaryExpr(Token oper, BoundExpression left, BoundExpression right)
    : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Binary;
    public Token Operator { get; } = oper;
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;
}
