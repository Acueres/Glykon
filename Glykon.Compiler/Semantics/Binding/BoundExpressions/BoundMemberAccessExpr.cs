namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundMemberAccessExpr(BoundExpression receiver, int nameId) : BoundExpression
{
    public override BoundExpressionKind Kind => BoundExpressionKind.MemberAccess;
    public BoundExpression Receiver { get; } = receiver;
    public int NameId { get; } = nameId;
}