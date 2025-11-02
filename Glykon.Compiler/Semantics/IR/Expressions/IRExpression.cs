using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public enum IRExpressionKind : byte
{
    Invalid,
    Unary,
    Binary,
    Call,
    Grouping,
    Literal,
    Variable,
    Assignment,
    Logical,
    Conversion
}

public abstract class IRExpression(TypeSymbol type)
{
    public abstract IRExpressionKind Kind { get; }
    public TypeSymbol Type { get; } = type;
}