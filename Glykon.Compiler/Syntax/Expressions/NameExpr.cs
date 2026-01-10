namespace Glykon.Compiler.Syntax.Expressions;

public class NameExpr(string name) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Name;
    public string Name { get; } = name;
}
