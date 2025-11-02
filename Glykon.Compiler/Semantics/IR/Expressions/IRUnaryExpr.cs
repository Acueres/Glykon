using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRUnaryExpr(Token oper, IRExpression operand, TypeSymbol type) : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Unary;
    public Token Operator { get; } = oper;
    public IRExpression Operand { get; } = operand;
}
