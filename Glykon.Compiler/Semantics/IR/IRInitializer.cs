using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR;

public class IRInitializer(FieldSymbol field, IRExpression value)
{
    public FieldSymbol Field { get; } = field;
    public IRExpression Value { get; } = value;
}