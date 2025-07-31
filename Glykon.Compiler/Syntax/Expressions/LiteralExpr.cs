namespace Glykon.Compiler.Syntax.Expressions
{
    public class LiteralExpr(Token token) : IExpression
    {
        public ExpressionType Type => ExpressionType.Literal;
        public Token Token { get; } = token;
    }
}
