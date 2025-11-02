using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements;

public class WhileStmt(Expression condition, Statement body) : Statement
{
    public override StatementKind Kind => StatementKind.While;
    public Expression Condition { get; } = condition;
    public Statement Body { get; } = body;
}
