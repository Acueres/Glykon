using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundAssignmentExpr(BoundExpression value, Symbol symbol) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Assignment;
    public BoundExpression Right { get; } = value;
    public Symbol Symbol { get; } = symbol;
}
