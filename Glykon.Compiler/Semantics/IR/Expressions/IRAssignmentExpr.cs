using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRAssignmentExpr(IRExpression value, Symbol symbol) : IRExpression(symbol.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Assignment;
    public IRExpression Value { get; } = value;
    public Symbol Symbol { get; } = symbol;
}
