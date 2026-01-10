using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRCallExpr(Symbol callable, IRExpression[] parameters) : IRExpression(callable.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Call;
    public Symbol Callable { get; } = callable;
    public IRExpression[] Parameters { get; } = parameters;
}