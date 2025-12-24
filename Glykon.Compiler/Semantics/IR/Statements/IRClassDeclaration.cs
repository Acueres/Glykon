using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRClassDeclaration(
    TypeSymbol symbol,
    IRMethodDeclaration[] methods,
    IRFieldDeclaration[] fields,
    IRConstantDeclaration[] constants,
    IRStatement[] nested) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Class;

    public TypeSymbol Type { get; } = symbol;
    public IRMethodDeclaration[] Methods { get; } = methods;
    public IRFieldDeclaration[] Fields { get; } = fields;
    public IRConstantDeclaration[] Constants { get; } = constants;
    public IRStatement[] Nested { get; } = nested;
}