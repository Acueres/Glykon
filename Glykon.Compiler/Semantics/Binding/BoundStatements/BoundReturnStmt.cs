using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundReturnStmt(BoundExpression? value, FunctionSymbol containingFunction, Token token) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Return;
    public BoundExpression? Value { get; } = value;
    public FunctionSymbol  ContainingFunction { get; } = containingFunction;
    public Token Token { get; } = token;
}
