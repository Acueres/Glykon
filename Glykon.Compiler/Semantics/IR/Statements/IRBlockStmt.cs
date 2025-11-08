using Glykon.Compiler.Semantics.Binding;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRBlockStmt(IRStatement[] statements, Scope scope) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Block;
    public Scope Scope { get; } = scope;
    public IRStatement[] Statements { get; } = statements;
}
