using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundUnaryExpr(Token oper, BoundExpression operand) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Unary;
    public Token Operator { get; } = oper;
    public BoundExpression Operand { get; } = operand;
}
