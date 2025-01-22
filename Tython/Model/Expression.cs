namespace Tython.Model
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

    public class CallExpr(IExpression callee, Token closingParenthesis, List<IExpression> args) : IExpression
    {
        public ExpressionType Type => ExpressionType.Call;
        public IExpression Callee { get; } = callee;
        public Token ClosingParenthesis { get; } = closingParenthesis;
        public List<IExpression> Args { get; } = args;
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

    public class LogicalExpr(Token oper, IExpression left, IExpression right) : IExpression
    {
        public ExpressionType Type => ExpressionType.Logical;
        public Token Operator { get; } = oper;
        public IExpression Left { get; } = left;
        public IExpression Right { get; } = right;
    }
}
