using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundBlockStmt(BoundStatement[] statements, Scope scope) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Block;
    public Scope Scope { get; } = scope;
    public BoundStatement[] Statements { get; } = statements;
}
