namespace Glykon.Compiler.Syntax.Statements;

public class FunctionDeclaration(string name, Parameter[] parameters, TypeAnnotation returnType, BlockStmt body) : Statement
{
    public override StatementKind Kind => StatementKind.Function;
    public string Name { get; } = name;
    public Parameter[] Parameters { get; } = parameters;
    public TypeAnnotation ReturnType { get; } = returnType;
    public BlockStmt Body { get; } = body;
}
