using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements;

public class ConstantDeclaration(Expression initializer, string name, TypeAnnotation declaredType) : Statement
{
    public override StatementKind Kind => StatementKind.Constant;
    public Expression Initializer { get; } = initializer;
    public string Name { get; } = name;
    public TypeAnnotation DeclaredType { get; } = declaredType;
}
