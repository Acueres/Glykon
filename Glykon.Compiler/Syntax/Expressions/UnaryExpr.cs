using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.Syntax.Expressions
{
    public class UnaryExpr(Token oper, IExpression expr) : IExpression
    {
        public ExpressionType Type => ExpressionType.Unary;
        public Token Operator { get; } = oper;
        public IExpression Expression { get; } = expr;
    }
}
