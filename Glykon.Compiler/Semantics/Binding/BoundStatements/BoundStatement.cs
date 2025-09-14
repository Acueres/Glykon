using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public abstract class BoundStatement
{
    public abstract StatementKind Kind { get; }
}
