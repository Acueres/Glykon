using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class ExpressionStmt(Expression expr) : Statement
    {
        public override StatementKind Kind => StatementKind.Expression;
        public Expression Expression { get; } = expr;
    }
}
