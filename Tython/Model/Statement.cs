namespace Tython.Model
{
    public class Statement(Token token, Expression expr)
    {
        public readonly Token token = token;
        public readonly Expression Expression = expr;
    }
}
