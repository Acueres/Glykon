using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements;

public class VariableDeclaration(Expression expr, string name, TypeAnnotation varType) : Statement
{
    public override StatementKind Kind => StatementKind.Variable;
    public Expression Expression { get; } = expr;
    public string Name { get; } = name;
    public TypeAnnotation DeclaredType { get; } = varType;
}
