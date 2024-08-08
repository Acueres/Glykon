using Tython.Enum;

namespace Tython.Model
{
    public interface IExpression
    {
        ExpressionType Type { get; }
    }

    public class UnaryExpr(Token oper, IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Unary;
        public Token Operator => oper;
        public IExpression Expr => expr;
    }

    public class BinaryExpr(Token oper, IExpression left, IExpression right) : IExpression
    {
        public ExpressionType Type => ExpressionType.Binary;
        public Token Operator => oper;
        public IExpression Left => left;
        public IExpression Right => right;
    }

    public class GroupingExpr(IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Grouping;
        public IExpression Expr => expr;
    }

    public class LiteralExpr(Token token) : IExpression
    {
        public ExpressionType Type => ExpressionType.Literal;
        public Token Token => token;
    }

    public class VariableExpr(string name) : IExpression
    {
        public ExpressionType Type => ExpressionType.Variable;
        public string Name => name;
    }
}
