namespace Tython.Model
{
    public class Statement(Token token, Expression expr)
    {
        public Token Token { get; } = token;
        public Expression Expression { get; } = expr;
    }
}
