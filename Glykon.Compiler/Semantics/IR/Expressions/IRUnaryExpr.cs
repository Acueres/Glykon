using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRUnaryExpr(UnaryOp oper, IRExpression operand, TypeSymbol type) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Unary;
    public UnaryOp Operator { get; } = oper;
    public IRExpression Operand { get; } = operand;
}
