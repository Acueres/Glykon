namespace CompilerService.Syntax.Expressions
{
    public class VariableExpr(string name) : IExpression
    {
        public ExpressionType Type => ExpressionType.Variable;
        public string Name { get; set; } = name;
    }
}
