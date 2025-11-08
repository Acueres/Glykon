using Glykon.Compiler.Semantics.IR.Expressions;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRExpressionStmt(IRExpression expr) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Expression;
    public IRExpression Expression { get; } = expr;
}
