using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundLogicalExpr(Token oper, BoundExpression left, BoundExpression right) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Logical;
    public Token Operator { get; } = oper;
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;
}
