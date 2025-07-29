using CompilerService.Syntax.Expressions;
using CompilerService.Tokenization;

namespace CompilerService.Syntax.Statements
{
    public class ConstantStmt(IExpression expr, string name, TokenType varType) : IStatement
    {
        public StatementType Type => StatementType.Constant;
        public IExpression Expression { get; } = expr;
        public string Name { get; } = name;
        public TokenType ConstantType { get; set; } = varType;
    }
}
