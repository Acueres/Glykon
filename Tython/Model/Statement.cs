using Tython.Enum;

namespace Tython.Model
{
    public class Statement(Token token, Expression expr, StatementType type)
    {
        public Token Token => token;
        public Expression Expression => expr;
        public StatementType Type => type;
    }
}
