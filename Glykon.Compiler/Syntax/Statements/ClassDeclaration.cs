namespace Glykon.Compiler.Syntax.Statements;

public class ClassDeclaration(
    string name,
    MethodDeclaration[] methods,
    FieldDeclaration[] fields,
    ConstantDeclaration[] constants,
    Statement[] nested) : Statement
{
    public override StatementKind Kind => StatementKind.Class;

    public string Name { get; } = name;
    public MethodDeclaration[] Methods { get; } = methods;
    public FieldDeclaration[] Fields { get; } = fields;
    public ConstantDeclaration[] Constants { get; } = constants;
    public Statement[] Nested { get; } = nested;
}