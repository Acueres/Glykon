namespace Glykon.Compiler.Syntax.Expressions;

public class VariableExpr(string name) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Variable;
    public string Name { get; set; } = name;
}
