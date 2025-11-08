using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundExpressionStmt(BoundExpression expr) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Expression;
    public BoundExpression Expression { get; } = expr;
}
