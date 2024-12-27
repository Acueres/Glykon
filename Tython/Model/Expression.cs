namespace Tython.Model
{
    public enum ExpressionType : byte
    {
        Unary,
        Binary,
        Grouping,
        Literal,
        Variable,
        Assignment
    }

    public interface IExpression
    {
        ExpressionType Type { get; }
    }

    public class UnaryExpr(Token oper, IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Unary;
        public Token Operator { get; } = oper;
        public IExpression Expr { get; } = expr;
    }

    public class BinaryExpr(Token oper, IExpression left, IExpression right) : IExpression
    {
        public ExpressionType Type => ExpressionType.Binary;
        public Token Operator { get; } = oper;
        public IExpression Left { get; } = left;
        public IExpression Right { get; } = right;
    }

    public class GroupingExpr(IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Grouping;
        public IExpression Expr { get; } = expr;
    }

    public class LiteralExpr(Token token) : IExpression
    {
        public ExpressionType Type => ExpressionType.Literal;
        public Token Token { get; } = token;
    }

    public class VariableExpr(string name) : IExpression
    {
        public ExpressionType Type => ExpressionType.Variable;
        public string Name { get; } = name;
    }

    public class AssignmentExpr(string name, IExpression value) : IExpression
    {
        public ExpressionType Type => ExpressionType.Assignment;
        public string Name { get; } = name;
        public IExpression Right { get; } = value;
    }
}
