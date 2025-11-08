namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundInvalidStmt : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Invalid;
}