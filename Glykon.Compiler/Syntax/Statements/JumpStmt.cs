namespace Glykon.Compiler.Syntax.Statements
{
    public class JumpStmt(Token token): Statement
    {
        public override StatementKind Kind => StatementKind.Jump;
        public Token Token { get; } = token;
    }
}
