namespace Glykon.Compiler.Semantics.IR.Statements;

public enum IRStatementKind : byte
{
    Invalid,
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

public abstract class IRStatement
{
    public abstract IRStatementKind Kind { get; }
}
