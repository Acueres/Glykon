namespace Glykon.Compiler.Syntax.Expressions;

public class BinaryExpr(Token oper, Expression left, Expression right) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Binary;
    public Token Operator { get; } = oper;
    public Expression Left { get; } = left;
    public Expression Right { get; } = right;
}
