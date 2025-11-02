using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRVariableExpr(Symbol symbol) : IRExpression(symbol.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Variable;
    public Symbol Symbol { get; } = symbol;
}
