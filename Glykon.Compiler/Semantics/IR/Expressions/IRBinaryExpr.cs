using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR.Expressions;

public class IRBinaryExpr(Token oper, IRExpression left, IRExpression right, TypeSymbol type)
    : IRExpression(type)
{
    public override IRExpressionKind Kind => IRExpressionKind.Binary;
    public Token Operator { get; } = oper;
    public IRExpression Left { get; } = left;
    public IRExpression Right { get; } = right;
}
