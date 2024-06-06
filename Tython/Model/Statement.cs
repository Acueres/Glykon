namespace Tython.Model
{
    public class Statement(Token token, Expression expr, StatementType type)
    {
        public Token Token { get; } = token;
        public Expression Expression { get; } = expr;
        public StatementType Type { get; } = type;
    }
}
