using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class ReturnStmt(Expression? expression) : Statement
    {
        public override StatementKind Kind => StatementKind.Return;
        public Expression Expression { get; } = expression;
    }
}
