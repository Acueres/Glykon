using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRFieldDeclaration(IRExpression? initializer, FieldSymbol symbol) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Field;
    
    public IRExpression? Initializer { get; } = initializer;
    public FieldSymbol Symbol { get; } = symbol;
}