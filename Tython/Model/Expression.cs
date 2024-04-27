namespace Tython.Model
{
    public class Expression
    {
        public Token Token { get; }
        public Expression? Primary { get; }
        public Expression? Secondary { get; }
        public ExpressionType Type { get; }

        public Expression(Token oper, Expression expr, ExpressionType type)
        {
            Token = oper;
            Primary = expr;
            Type = type;
        }

        public Expression(Token oper, Expression left, Expression right, ExpressionType type)
        {
            Token = oper;
            Primary = left;
            Secondary = right;
            Type = type;
        }

        public Expression(Token token, ExpressionType type)
        {
            Token = token;
            Type = type;
        }

        public Expression(Expression expr, ExpressionType type)
        {
            Primary = expr;
            Type = type;
        }
    }
}
