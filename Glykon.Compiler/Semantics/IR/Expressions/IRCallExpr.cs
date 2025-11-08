using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRCallExpr(FunctionSymbol function, IRExpression[] parameters) : IRExpression(function.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Call;
    public FunctionSymbol Function { get; } = function;
    public IRExpression[] Parameters { get; } = parameters;
}