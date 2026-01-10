namespace Glykon.Compiler.Syntax.Statements;

public class MethodDeclaration(
    string name,
    Parameter[] parameters,
    TypeAnnotation returnType,
    BlockStmt body,
    bool isStatic)
    : FunctionDeclaration(name, parameters, returnType, body)
{
    public override StatementKind Kind => StatementKind.Method;

    public bool IsStatic { get; } = isStatic;
}