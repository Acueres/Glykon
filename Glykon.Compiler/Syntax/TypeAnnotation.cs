using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax;

public class TypeAnnotation(Expression expression)
{
    public Expression Expression { get; } = expression;

    public static TypeAnnotation None { get;  } = new(new NameExpr("none"));
}
