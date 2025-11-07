namespace Glykon.Compiler.Semantics.Operators;

public static class OpTraits
{
    public static bool IsArithmetic(BinaryOp op) =>
        op is BinaryOp.Add or BinaryOp.Subtract or BinaryOp.Multiply or BinaryOp.Divide;

    public static bool IsComparison(BinaryOp op) =>
        op is BinaryOp.Less or BinaryOp.LessOrEqual or BinaryOp.Greater or BinaryOp.GreaterOrEqual;

    public static bool IsEquality(BinaryOp op) =>
        op is BinaryOp.Equal or BinaryOp.NotEqual;

    public static bool IsLogical(BinaryOp op) =>
        op is BinaryOp.LogicalAnd or BinaryOp.LogicalOr;
}