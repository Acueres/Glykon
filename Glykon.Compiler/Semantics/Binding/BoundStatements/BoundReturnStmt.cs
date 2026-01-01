using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundReturnStmt(BoundExpression? value, Symbol containerSymbol, Token token) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Return;
    public BoundExpression? Value { get; } = value;
    public Symbol? ContainerSymbol { get; } = containerSymbol;
    public Token Token { get; } = token;
}
