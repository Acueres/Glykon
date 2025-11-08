using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRLogicalExpr(BinaryOp oper, IRExpression left, IRExpression right, TypeSymbol type)
    : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Logical;
    public BinaryOp Operator { get; } = oper;
    public IRExpression Left { get; } = left;
    public IRExpression Right { get; } = right;
}
