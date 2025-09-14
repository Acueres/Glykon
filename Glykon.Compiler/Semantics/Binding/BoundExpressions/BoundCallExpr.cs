using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundCallExpr(FunctionSymbol function, BoundExpression[] args) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Call;
    public FunctionSymbol Function { get; } = function;
    public BoundExpression[] Args { get; } = args;
}