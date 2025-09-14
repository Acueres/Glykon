using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class IfStmt(Expression condition, Statement statement, Statement? elseStatement) : Statement
    {
        public override StatementKind Kind => StatementKind.If;
        public Expression Condition { get; } = condition;
        public Statement ThenStatement { get; } = statement;
        public Statement? ElseStatement { get; } = elseStatement;
    }
}
