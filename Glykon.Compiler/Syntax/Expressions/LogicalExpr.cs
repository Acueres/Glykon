namespace Glykon.Compiler.Syntax.Expressions;

public class LogicalExpr(Token oper, Expression left, Expression right) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Logical;
    public Token Operator { get; } = oper;
    public Expression Left { get; } = left;
    public Expression Right { get; } = right;
}
