using CompilerService.Syntax.Expressions;

namespace CompilerService.Syntax.Statements
{
    public class ReturnStmt(IExpression? expression) : IStatement
    {
        public StatementType Type => StatementType.Return;
        public IExpression Expression { get; } = expression;
    }
}
