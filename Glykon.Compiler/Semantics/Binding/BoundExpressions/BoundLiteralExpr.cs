using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundLiteralExpr(Token token) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Literal;
    public Token Token { get; } = token;
}
