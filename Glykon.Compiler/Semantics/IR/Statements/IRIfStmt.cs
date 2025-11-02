using Glykon.Compiler.Semantics.IR.Expressions;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRIfStmt(IRExpression condition, IRStatement statement, IRStatement? elseStatement) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.If;
    public IRExpression Condition { get; } = condition;
    public IRStatement ThenStatement { get; } = statement;
    public IRStatement? ElseStatement { get; } = elseStatement;
}
