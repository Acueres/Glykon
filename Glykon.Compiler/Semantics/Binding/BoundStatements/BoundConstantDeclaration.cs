using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundConstantDeclaration(BoundExpression expr, string name, Token token, TokenKind varType) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Constant;
    public BoundExpression Expression { get; } = expr;
    public string Name { get; } = name;
    public TokenKind ConstantType { get; } = varType;
    public Token Token { get; } = token;
}
