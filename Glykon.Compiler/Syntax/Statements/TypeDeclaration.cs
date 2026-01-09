namespace Glykon.Compiler.Syntax.Statements;

public class TypeDeclaration(
    string name, bool isValueType,
    MethodDeclaration[] methods,
    FieldDeclaration[] fields,
    ConstantDeclaration[] constants,
    TypeDeclaration[] nested) : Statement
{
    public override StatementKind Kind => StatementKind.Type;

    public string Name { get; } = name;
    public bool IsValueType { get; } = isValueType;
    public MethodDeclaration[] Methods { get; } = methods;
    public FieldDeclaration[] Fields { get; } = fields;
    public ConstantDeclaration[] Constants { get; } = constants;
    public TypeDeclaration[] Nested { get; } = nested;
}