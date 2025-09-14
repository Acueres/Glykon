using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundUnaryExpr(Token oper, BoundExpression expr) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Unary;
    public Token Operator { get; } = oper;
    public BoundExpression Operand { get; } = expr;
}
