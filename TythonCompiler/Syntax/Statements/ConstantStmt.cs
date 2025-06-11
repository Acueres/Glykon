using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Tokenization;

namespace TythonCompiler.Syntax.Statements
{
    public class ConstantStmt(IExpression expr, string name, TokenType varType) : IStatement
    {
        public StatementType Type => StatementType.Constant;
        public IExpression Expression { get; } = expr;
        public string Name { get; } = name;
        public TokenType ConstantType { get; set; } = varType;
    }
}
