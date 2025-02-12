using TythonCompiler.Syntax.Expressions;

namespace TythonCompiler.Syntax.Statements
{
    public class ExpressionStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Expression;
        public IExpression Expression { get; } = expr;
    }
}
