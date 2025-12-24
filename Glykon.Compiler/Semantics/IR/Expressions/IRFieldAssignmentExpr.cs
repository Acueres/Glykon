using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRFieldAssignmentExpr(IRExpression receiver, FieldSymbol field, IRExpression value)
    : IRExpression(field.Type)
{
    public override IRExpressionKind Kind => IRExpressionKind.FieldAssignment;
    public IRExpression Receiver { get; } = receiver;
    public FieldSymbol Field { get; } = field;
    public IRExpression Value { get; } = value;
}