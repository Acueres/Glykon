namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRInvalidStmt : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Invalid;
}