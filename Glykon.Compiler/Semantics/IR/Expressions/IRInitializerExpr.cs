using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRInitializerExpr(TypeSymbol type, IRInitializer[] initializers) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Initializer;
    public IRInitializer[] Initializers { get; } = initializers;
}