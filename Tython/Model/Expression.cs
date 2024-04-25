namespace Tython.Model
{
    public class Expression
    {
        public Token Operator { get; }
        public Expression? Left { get; }
        public Expression? Right { get; }
        public ExpressionType Type { get; }

        public Expression(Token oper, Expression expression, ExpressionType type)
        {
            Operator = oper;
            Left = expression;
            Type = type;
        }

        public Expression(Token oper, Expression left, Expression right, ExpressionType type)
        {
            Operator = oper;
            Left = left;
            Right = right;
            Type = type;
        }

        public Expression(Token oper, ExpressionType type)
        {
            Operator = oper;
            Type = type;
        }

        public Expression(Expression expr, ExpressionType type)
        {
            Left = expr;
            Type = type;
        }
    }
}
