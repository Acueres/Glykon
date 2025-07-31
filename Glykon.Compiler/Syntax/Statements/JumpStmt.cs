using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.Syntax.Statements
{
    public class JumpStmt(Token token): IStatement
    {
        public StatementType Type => StatementType.Jump;
        public IExpression Expression { get; }
        public Token Token { get; } = token;
    }
}
