using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundNameExpr(Symbol symbol) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Name;
    public Symbol Symbol { get; } = symbol;
}
