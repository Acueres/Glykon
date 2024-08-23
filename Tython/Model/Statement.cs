namespace Tython.Model
{
    public interface IStatement
    {
        StatementType Type { get; }
        IExpression Expression { get; }
    }

    public class ExpressionStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Expression;
        public IExpression Expression => expr;
    }

    public class PrintStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Print;
        public IExpression Expression => expr;
    }

    public class VariableStmt(IExpression expr, string name, TokenType varType) : IStatement
    {
        public StatementType Type => StatementType.Variable;
        public IExpression Expression => expr;
        public string Name => name;
        public TokenType VariableType => varType;
    }
}
