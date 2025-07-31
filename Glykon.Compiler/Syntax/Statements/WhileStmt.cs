using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class WhileStmt(IExpression condition, IStatement statement) : IStatement
    {
        public StatementType Type => StatementType.While;
        public IExpression Expression { get; } = condition;
        public IStatement Statement { get; } = statement;
    }
}
