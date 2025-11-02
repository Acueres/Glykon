using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundConstantDeclaration(BoundExpression initializer, ConstantSymbol symbol) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Constant;
    public BoundExpression Initializer { get; } = initializer;
    public ConstantSymbol Symbol { get; } = symbol;
}
