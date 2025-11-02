using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRLiteralExpr(ConstantValue value, TypeSymbol type) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Literal;
    public ConstantValue Value { get; } = value;
}
