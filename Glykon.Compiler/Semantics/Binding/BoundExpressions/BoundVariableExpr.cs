using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Binding.BoundExpressions;

public class BoundVariableExpr(string name, Symbol symbol) : BoundExpression
{
    public override ExpressionKind Kind => ExpressionKind.Variable;
    public string Name { get; } = name;
    public Symbol Symbol { get; } = symbol;
}
