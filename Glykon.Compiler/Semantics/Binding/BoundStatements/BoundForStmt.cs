using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundForStmt(BoundVariableDeclaration iter, BoundRangeExpr range, BoundStatement body) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.For;

    public BoundVariableDeclaration Iterator { get; } = iter;
    public BoundRangeExpr Range { get; } = range;
    public BoundStatement Body { get; } = body;
}