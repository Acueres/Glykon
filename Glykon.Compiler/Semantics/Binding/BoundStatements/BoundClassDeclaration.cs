using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundClassDeclaration(
    TypeSymbol symbol,
    BoundMethodDeclaration[] methods,
    BoundFieldDeclaration[] fields,
    BoundConstantDeclaration[] constants,
    BoundClassDeclaration[] nested) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Class;

    public TypeSymbol Type { get; } = symbol;
    public BoundMethodDeclaration[] Methods { get; } = methods;
    public BoundFieldDeclaration[] Fields { get; } = fields;
    public BoundConstantDeclaration[] Constants { get; } = constants;
    public BoundClassDeclaration[] Nested { get; } = nested;
}