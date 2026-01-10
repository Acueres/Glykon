namespace Glykon.Compiler.Semantics.IR.Statements;

public enum IRStatementKind : byte
{
    Invalid,
    Block,
    Expression,
    Variable,
    Field,
    Constant,
    Function,
    Method,
    Type,
    Return,
    If,
    While,
    For,
    Jump
}

public abstract class IRStatement
{
    public abstract IRStatementKind Kind { get; }
}
