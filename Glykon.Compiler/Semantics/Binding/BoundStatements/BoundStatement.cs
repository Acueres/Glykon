namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public enum BoundStatementKind : byte
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

public abstract class BoundStatement
{
    public abstract BoundStatementKind Kind { get; }
}
