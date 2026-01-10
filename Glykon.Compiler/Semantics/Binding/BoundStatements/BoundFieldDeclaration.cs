using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundFieldDeclaration(BoundExpression? initializer, FieldSymbol symbol) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Field;
    
    public BoundExpression? Initializer { get; } = initializer;
    public FieldSymbol Symbol { get; } = symbol;
}