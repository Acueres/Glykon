namespace TythonCompiler.Syntax.Expressions
{
    public enum ExpressionType : byte
    {
        Unary,
        Binary,
        Call,
        Grouping,
        Literal,
        Variable,
        Assignment,
        Logical
    }

    public interface IExpression
    {
        ExpressionType Type { get; }
    }
}
