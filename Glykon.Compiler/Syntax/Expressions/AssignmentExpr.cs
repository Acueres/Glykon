namespace Glykon.Compiler.Syntax.Expressions;

public class AssignmentExpr(string name, Expression value) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Assignment;
    public string Name { get; } = name;
    public Expression Right { get; } = value;
}
