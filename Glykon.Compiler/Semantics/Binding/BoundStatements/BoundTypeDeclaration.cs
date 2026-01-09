using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundTypeDeclaration(
    TypeSymbol symbol,
    BoundMethodDeclaration[] methods,
    BoundFieldDeclaration[] fields,
    BoundConstantDeclaration[] constants,
    BoundTypeDeclaration[] nested) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Type;

    public TypeSymbol Type { get; } = symbol;
    public BoundMethodDeclaration[] Methods { get; } = methods;
    public BoundFieldDeclaration[] Fields { get; } = fields;
    public BoundConstantDeclaration[] Constants { get; } = constants;
    public BoundTypeDeclaration[] Nested { get; } = nested;
}