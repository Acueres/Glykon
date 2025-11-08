using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRBinaryExpr(BinaryOp oper, IRExpression left, IRExpression right, TypeSymbol type)
    : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Binary;
    public BinaryOp Operator { get; } = oper;
    public IRExpression Left { get; } = left;
    public IRExpression Right { get; } = right;
}
