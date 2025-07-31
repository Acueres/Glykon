using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class ReturnStmt(IExpression? expression) : IStatement
    {
        public StatementType Type => StatementType.Return;
        public IExpression Expression { get; } = expression;
    }
}
