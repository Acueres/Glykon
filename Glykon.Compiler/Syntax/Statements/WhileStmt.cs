using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class WhileStmt(Expression condition, Statement statement) : Statement
    {
        public override StatementKind Kind => StatementKind.While;
        public Expression Condition { get; } = condition;
        public Statement Statement { get; } = statement;
    }
}
