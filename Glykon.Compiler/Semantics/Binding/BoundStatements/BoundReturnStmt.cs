using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundReturnStmt(BoundExpression? expression, Token token) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Return;
    public BoundExpression? Expression { get; } = expression;
    public Token Token { get; } = token;
}
