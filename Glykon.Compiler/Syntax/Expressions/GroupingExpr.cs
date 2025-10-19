namespace Glykon.Compiler.Syntax.Expressions;

public class GroupingExpr(Expression expr) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Grouping;
    public Expression Expression { get; } = expr;
}
