using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundReturnStmt(BoundExpression? expression) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Return;
    public BoundExpression Expression { get; } = expression;
}
