using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRConversionExpr(IRExpression expression, TypeSymbol targetType) : IRExpression(targetType)
{
    public override IRExpressionKind Kind => IRExpressionKind.Conversion;
    public IRExpression Expression { get; } =  expression;
}