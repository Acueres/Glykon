using Tython.Enum;

namespace Tython.Model
{
    public class Statement(Token token, IExpression expr, StatementType type)
    {
        public Token Token => token;
        public IExpression Expression => expr;
        public StatementType Type => type;
    }
}
