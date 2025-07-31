namespace Glykon.Compiler.Syntax.Expressions
{
    public class GroupingExpr(IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Grouping;
        public IExpression Expression { get; } = expr;
    }
}
