namespace Glykon.Compiler.Semantics.Operators;

public enum BinaryOp : byte
{
    Add, Subtract, Multiply, Divide,
    Less, LessOrEqual, Greater, GreaterOrEqual,
    Equal, NotEqual,
    LogicalAnd, LogicalOr
}