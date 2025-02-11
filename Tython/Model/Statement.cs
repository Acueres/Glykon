namespace Tython.Model
{
    public enum StatementType : byte
    {
        Block,
        Expression,
        Print,
        Variable,
        Function,
        Return,
        If,
        While,
        Jump
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

    public class FunctionStmt(string name, List<Parameter> parameters, TokenType returnType, BlockStmt body) : IStatement
    {
        public StatementType Type => StatementType.Function;
        public IExpression Expression { get; }
        public string Name { get; } = name;
        public List<Parameter> Parameters { get; } = parameters;
        public TokenType ReturnType { get; } = returnType;
        public BlockStmt Body { get; } = body;
    }

    public class ReturnStmt(IExpression expression) : IStatement
    {
        public StatementType Type => StatementType.Return;
        public IExpression Expression { get; } = expression;
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

    public class JumpStmt(Token token): IStatement
    {
        public StatementType Type => StatementType.Jump;
        public IExpression Expression { get; }
        public Token Token { get; } = token;
    }
}
