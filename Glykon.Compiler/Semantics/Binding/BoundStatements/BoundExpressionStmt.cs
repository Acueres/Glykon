using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundExpressionStmt(BoundExpression expr) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Expression;
    public BoundExpression Expression { get; } = expr;
}
