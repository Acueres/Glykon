using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public enum JumpKind : byte
{
    None,
    Break,
    Continue
}

public class BoundJumpStmt(TokenKind tokenKind): BoundStatement
{
    public override StatementKind Kind => StatementKind.Jump;
    public JumpKind JumpKind = GetJumpKind(tokenKind);

    static JumpKind GetJumpKind(TokenKind tokenKind)
    {
        if (tokenKind == TokenKind.Break) return JumpKind.Break;
        if (tokenKind == TokenKind.Continue) return JumpKind.Continue;
        return JumpKind.None;
    }
}
