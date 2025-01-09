namespace Tython.Model
{
    public enum StatementType : byte
    {
        Block,
        Expression,
        Print,
        Variable,
        If,
        While
    }

    public interface IStatement
    {
        StatementType Type { get; }
        IExpression Expression { get; }
    }

    public class BlockStmt(List<IStatement> statements, int scopeIndex) : IStatement
    {
        public StatementType Type => StatementType.Block;
        public IExpression Expression { get; }
        public int ScopeIndex { get; } = scopeIndex;
        public List<IStatement> Statements { get; } = statements;
    }

    public class ExpressionStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Expression;
        public IExpression Expression { get; } = expr;
    }

    public class PrintStmt(IExpression expr) : IStatement
    {
        public StatementType Type => StatementType.Print;
        public IExpression Expression { get; } = expr;
    }

    public class VariableStmt(IExpression expr, string name, TokenType varType) : IStatement
    {
        public StatementType Type => StatementType.Variable;
        public IExpression Expression { get; } = expr;
        public string Name { get; } = name;
        public TokenType VariableType { get; } = varType;
    }

    public class IfStmt(IExpression condition, IStatement statement, IStatement? elseStatement) : IStatement
    {
        public StatementType Type => StatementType.If;
        public IExpression Expression { get; } = condition;
        public IStatement Statement { get; } = statement;
        public IStatement? ElseStatement { get; } = elseStatement;
    }

    public class WhileStmt(IExpression condition, IStatement statement) : IStatement
    {
        public StatementType Type => StatementType.While;
        public IExpression Expression { get; } = condition;
        public IStatement Statement { get; } = statement;
    }
}
