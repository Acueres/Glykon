namespace Glykon.Compiler.Syntax.Statements;

public class FunctionDeclaration(string name, List<(string, TokenKind)> parameters, TokenKind returnType, BlockStmt body) : Statement
{
    public override StatementKind Kind => StatementKind.Function;
    public string Name { get; set; } = name;
    public List<(string Name, TokenKind Type)> Parameters { get; } = parameters;
    public TokenKind ReturnType { get; } = returnType;
    public BlockStmt Body { get; } = body;
}
