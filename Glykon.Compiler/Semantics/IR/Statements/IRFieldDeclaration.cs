using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRFieldDeclaration(FieldSymbol symbol) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Field;
    public FieldSymbol Symbol { get; } = symbol;
}