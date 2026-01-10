using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding;

public class BoundInitializer(FieldSymbol field, BoundExpression value)
{
    public FieldSymbol Field { get; } = field;
    public BoundExpression Value { get; } = value;
}