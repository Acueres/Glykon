using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRConstantDeclaration(IRExpression initializer, ConstantSymbol symbol) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Constant;
    public IRExpression Initializer { get; } = initializer;
    public ConstantSymbol Symbol { get; } = symbol;
}
