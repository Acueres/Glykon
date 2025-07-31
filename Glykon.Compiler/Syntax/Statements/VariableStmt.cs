using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class VariableStmt(IExpression expr, string name, TokenType varType) : IStatement
    {
        public StatementType Type => StatementType.Variable;
        public IExpression Expression { get; } = expr;
        public string Name { get; } = name;
        public TokenType VariableType { get; set; } = varType;
    }
}
