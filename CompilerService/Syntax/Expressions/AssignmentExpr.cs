namespace CompilerService.Syntax.Expressions
{
    public class AssignmentExpr(string name, IExpression value) : IExpression
    {
        public ExpressionType Type => ExpressionType.Assignment;
        public string Name { get; } = name;
        public IExpression Right { get; } = value;
    }
}
