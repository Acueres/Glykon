using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRTypeDeclaration(
    TypeSymbol symbol,
    IRMethodDeclaration[] methods,
    IRFieldDeclaration[] fields,
    IRConstantDeclaration[] constants,
    IRTypeDeclaration[] nested) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Type;

    public TypeSymbol Type { get; } = symbol;
    public IRMethodDeclaration[] Methods { get; } = methods;
    public IRFieldDeclaration[] Fields { get; } = fields;
    public IRConstantDeclaration[] Constants { get; } = constants;
    public IRTypeDeclaration[] Nested { get; } = nested;
}