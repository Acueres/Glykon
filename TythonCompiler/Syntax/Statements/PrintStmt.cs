using TythonCompiler.Syntax.Expressions;

namespace TythonCompiler.Syntax.Statements
{
    public class PrintStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Print;
        public IExpression Expression { get; } = expr;
    }
}
