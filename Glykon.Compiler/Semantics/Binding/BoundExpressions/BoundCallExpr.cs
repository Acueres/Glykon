using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundCallExpr(int nameId, FunctionSymbol[] overloads, BoundExpression[] parameters) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.Call;
    public int NameId { get; } = nameId;
    public FunctionSymbol[] Overloads { get; } = overloads;
    public BoundExpression[] Parameters { get; } = parameters;
}