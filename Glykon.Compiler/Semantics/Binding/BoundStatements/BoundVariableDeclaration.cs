using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundVariableDeclaration(BoundExpression initializer, VariableSymbol symbol, TypeSymbol varType) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Variable;
    public BoundExpression Initializer { get; } = initializer;
    public VariableSymbol Symbol { get; } = symbol;
    public TypeSymbol DeclaredType { get; } = varType;
}
