using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRInvalidExpr(TypeSymbol type) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Invalid;
}