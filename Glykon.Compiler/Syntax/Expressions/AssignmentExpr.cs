namespace Glykon.Compiler.Syntax.Expressions;

public class AssignmentExpr(Expression target, Expression value) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Assignment;
    public Expression Target { get; } = target;
    public Expression Value { get; } = value;
}
