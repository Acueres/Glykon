using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundAssignmentExpr(BoundExpression value, Symbol symbol) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Assignment;
    public BoundExpression Value { get; } = value;
    public Symbol Symbol { get; } = symbol;
}
