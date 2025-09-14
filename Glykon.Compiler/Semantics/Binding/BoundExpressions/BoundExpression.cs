using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public abstract class BoundExpression
{
    public abstract ExpressionKind Kind { get; }
}