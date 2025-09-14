using Glykon.Compiler.Semantics;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class BlockStmt(List<Statement> statements) : Statement
    {
        public override StatementKind Kind => StatementKind.Block;
        public List<Statement> Statements { get; } = statements;
    }
}
