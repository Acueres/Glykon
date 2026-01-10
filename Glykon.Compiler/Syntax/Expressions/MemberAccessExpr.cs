namespace Glykon.Compiler.Syntax.Expressions;

public class MemberAccessExpr(Expression receiver, string name) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.MemberAccess;

    public Expression Receiver { get; } = receiver;
    public string Name { get; } = name;
}