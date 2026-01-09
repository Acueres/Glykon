namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public enum BoundStatementKind : byte
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

public abstract class BoundStatement
{
    public abstract BoundStatementKind Kind { get; }
}
