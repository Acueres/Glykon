using CompilerService.Syntax.Expressions;
using CompilerService.Tokenization;

namespace CompilerService.Syntax.Statements
{
    public class JumpStmt(Token token): IStatement
    {
        public StatementType Type => StatementType.Jump;
        public IExpression Expression { get; }
        public Token Token { get; } = token;
    }
}
