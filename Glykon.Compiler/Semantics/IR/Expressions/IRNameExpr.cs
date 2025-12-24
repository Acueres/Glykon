using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRNameExpr(Symbol symbol) : IRExpression(symbol.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Name;
    public Symbol Symbol { get; } = symbol;
}
