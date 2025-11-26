using Glykon.Compiler.Semantics.IR.Expressions;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRWhileStmt(IRExpression condition, IRStatement body) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.While;
    public IRExpression Condition { get; } = condition;
    public IRStatement Body { get; } = body;
}
