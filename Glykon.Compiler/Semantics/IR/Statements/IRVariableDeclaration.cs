using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRVariableDeclaration(IRExpression initializer, VariableSymbol symbol) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Variable;
    public IRExpression Initializer { get; } = initializer;
    public VariableSymbol Symbol { get; } = symbol;
}
