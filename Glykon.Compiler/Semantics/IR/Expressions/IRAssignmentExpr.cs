using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRAssignmentExpr(IRExpression value, Symbol symbol, TypeSymbol type) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Assignment;
    public IRExpression Value { get; } = value;
    public Symbol Symbol { get; } = symbol;
}
