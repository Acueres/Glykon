using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundBinaryExpr(Token oper, BoundExpression left, BoundExpression right) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Binary;
    public Token Operator { get; } = oper;
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;
}
