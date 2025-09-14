using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundVariableDeclaration(BoundExpression expr, string name, TokenKind varType) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Variable;
    public BoundExpression Expression { get; } = expr;
    public string Name { get; } = name;
    public TokenKind VariableType { get; } = varType;
}
