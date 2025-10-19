using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundVariableDeclaration(BoundExpression expr, VariableSymbol symbol, TypeSymbol varType) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Variable;
    public BoundExpression Expression { get; } = expr;
    public VariableSymbol Symbol { get; } = symbol;
    public TypeSymbol VariableType { get; } = varType;
}
