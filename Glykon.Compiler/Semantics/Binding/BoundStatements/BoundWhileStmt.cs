using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundWhileStmt(BoundExpression condition, BoundStatement statement) : BoundStatement
{
    public override StatementKind Kind => StatementKind.While;
    public BoundExpression Condition { get; } = condition;
    public BoundStatement Body { get; } = statement;
}
