namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRGroupingExpr(IRExpression expr) : IRExpression(expr.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Grouping;
    public IRExpression Expression { get; } = expr;
}
