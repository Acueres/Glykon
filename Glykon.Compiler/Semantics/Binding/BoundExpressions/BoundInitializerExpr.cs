using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundInitializerExpr(TypeSymbol type, BoundInitializer[] initializers) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Initializer;

    public TypeSymbol Type { get; } = type;
    public BoundInitializer[] Initializers { get; } = initializers;
}