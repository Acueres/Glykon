namespace Glykon.Compiler.Syntax.Statements;

public class FunctionDeclaration(string name, List<(string, TypeAnnotation)> parameters, TypeAnnotation returnType, BlockStmt body) : Statement
{
    public override StatementKind Kind => StatementKind.Function;
    public string Name { get; set; } = name;
    public List<(string Name, TypeAnnotation Type)> Parameters { get; } = parameters;
    public TypeAnnotation ReturnType { get; } = returnType;
    public BlockStmt Body { get; } = body;
}
