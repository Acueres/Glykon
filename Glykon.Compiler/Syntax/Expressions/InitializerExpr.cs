namespace Glykon.Compiler.Syntax.Expressions;

public class InitializerExpr(TypeAnnotation typeName, Initializer[] initializers) : Expression
{
    public override ExpressionKind Kind => ExpressionKind.Initializer;
    
    public TypeAnnotation TypeName { get; } = typeName;
    public Initializer[] Initializers { get; } = initializers;
}