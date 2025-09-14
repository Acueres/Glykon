using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundIfStmt(BoundExpression condition, BoundStatement statement, BoundStatement? elseStatement) : BoundStatement
{
    public override StatementKind Kind => StatementKind.If;
    public BoundExpression Expression { get; } = condition;
    public BoundStatement ThenStatement { get; } = statement;
    public BoundStatement? ElseStatement { get; } = elseStatement;
}
