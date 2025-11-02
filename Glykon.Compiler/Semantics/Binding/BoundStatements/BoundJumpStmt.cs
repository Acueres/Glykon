using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundJumpStmt(Token token): BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Jump;
    public Token Token { get; } = token;
    public bool IsBreak { get; } = token.Kind == TokenKind.Break;
    public bool IsContinue { get; } = token.Kind == TokenKind.Continue;
}
