using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements;

public class VariableDeclaration(Expression initializer, string name, TypeAnnotation declaredType, bool immutable) : Statement
{
    public override StatementKind Kind => StatementKind.Variable;
    public Expression Initializer { get; } = initializer;
    public string Name { get; } = name;
    public TypeAnnotation DeclaredType { get; } = declaredType;
    public bool Immutable { get; } = immutable;
}
