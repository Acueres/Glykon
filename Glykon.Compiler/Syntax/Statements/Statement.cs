namespace Glykon.Compiler.Syntax.Statements;

public enum StatementKind : byte
{
    Block,
    Expression,
    Variable,
    Constant,
    Function,
    Method,
    Class,
    Field,
    Return,
    If,
    While,
    For,
    Jump
}

public abstract class Statement
{
    public abstract StatementKind Kind { get; }
}
