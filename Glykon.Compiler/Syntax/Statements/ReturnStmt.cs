using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class ReturnStmt(Expression? expression, Token token) : Statement
    {
        public override StatementKind Kind => StatementKind.Return;
        public Expression Expression { get; } = expression;
        public Token Token { get; } = token;
    }
}
