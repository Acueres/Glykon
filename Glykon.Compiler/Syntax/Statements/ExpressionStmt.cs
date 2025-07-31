using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class ExpressionStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Expression;
        public IExpression Expression { get; } = expr;
    }
}
