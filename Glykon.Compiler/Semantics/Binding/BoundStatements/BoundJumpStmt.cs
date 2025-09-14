using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundJumpStmt(Token token): BoundStatement
{
    public override StatementKind Kind => StatementKind.Jump;
    public Token Token { get; } = token;
}
