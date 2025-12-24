namespace Glykon.Compiler.Syntax.Expressions;

public class ConversionExpr(Expression expr, TypeAnnotation targetType) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Conversion;
    public Expression Expression { get; } = expr;
    public TypeAnnotation TargetType { get; } = targetType;
}