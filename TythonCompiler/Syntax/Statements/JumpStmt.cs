using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Tokenization;

namespace TythonCompiler.Syntax.Statements
{
    public class JumpStmt(Token token): IStatement
    {
        public StatementType Type => StatementType.Jump;
        public IExpression Expression { get; }
        public Token Token { get; } = token;
    }
}
