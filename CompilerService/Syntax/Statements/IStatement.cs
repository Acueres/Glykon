using CompilerService.Syntax.Expressions;

namespace CompilerService.Syntax.Statements
{
    public enum StatementType : byte
    {
        Block,
        Expression,
        Variable,
        Constant,
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
}
