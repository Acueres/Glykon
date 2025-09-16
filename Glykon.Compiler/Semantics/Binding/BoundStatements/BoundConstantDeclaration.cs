using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundConstantDeclaration(ConstantSymbol symbol) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Constant;
    public ConstantSymbol Symbol { get; } = symbol;
}
