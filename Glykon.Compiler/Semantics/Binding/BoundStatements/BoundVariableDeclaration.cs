using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundVariableDeclaration(BoundExpression expr, VariableSymbol symbol, TokenKind varType) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Variable;
    public BoundExpression Expression { get; } = expr;
    public VariableSymbol Symbol { get; } = symbol;
    public TokenKind VariableType { get; } = varType;
}
