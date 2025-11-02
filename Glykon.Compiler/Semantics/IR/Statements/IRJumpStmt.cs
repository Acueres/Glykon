using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRJumpStmt(Token token): IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Jump;
    public Token Token { get; } = token;
    public bool IsBreak { get; } = token.Kind == TokenKind.Break;
    public bool IsContinue { get; } = token.Kind == TokenKind.Continue;
}
