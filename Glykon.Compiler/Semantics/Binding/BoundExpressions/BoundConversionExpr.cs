using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundConversionExpr(BoundExpression expression, TypeSymbol targetType) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Conversion;
    public BoundExpression Expression { get; } =  expression;
    public TypeSymbol TargetType { get; } =  targetType;
}