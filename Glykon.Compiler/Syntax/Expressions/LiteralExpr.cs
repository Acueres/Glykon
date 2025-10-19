using Glykon.Compiler.Core;

namespace Glykon.Compiler.Syntax.Expressions;

public class LiteralExpr(ConstantValue value) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Literal;
    public ConstantValue Value { get; } = value;
}
