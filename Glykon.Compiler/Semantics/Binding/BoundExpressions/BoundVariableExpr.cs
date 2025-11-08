using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundVariableExpr(Symbol symbol) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Variable;
    public Symbol Symbol { get; } = symbol;
}
