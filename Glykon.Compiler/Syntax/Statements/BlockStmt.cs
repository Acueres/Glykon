using Glykon.Compiler.Semantics;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class BlockStmt(List<IStatement> statements) : IStatement
    {
        public StatementType Type => StatementType.Block;
        public IExpression Expression { get; }
        public Scope Scope { get; set; }
        public List<IStatement> Statements { get; } = statements;
    }
}
