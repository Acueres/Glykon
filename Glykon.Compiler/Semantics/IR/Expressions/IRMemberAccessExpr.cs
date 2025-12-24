using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRMemberAccessExpr(IRExpression receiver, Symbol memberSymbol) : IRExpression(memberSymbol.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.MemberAccess;
    public IRExpression Receiver { get; } = receiver;
    public Symbol MemberSymbol { get; } = memberSymbol;
}