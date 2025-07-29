using CompilerService.Syntax.Expressions;

namespace CompilerService.Syntax.Statements
{
    public class IfStmt(IExpression condition, IStatement statement, IStatement? elseStatement) : IStatement
    {
        public StatementType Type => StatementType.If;
        public IExpression Expression { get; } = condition;
        public IStatement ThenStatement { get; } = statement;
        public IStatement? ElseStatement { get; } = elseStatement;
    }
}
