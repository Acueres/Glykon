using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRMethodDeclaration(
    MethodSymbol symbol,
    TypeSymbol parentType,
    ParameterSymbol[] parameters,
    TypeSymbol returnType,
    IRBlockStmt body,
    ParameterSymbol? thisParameter) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Method;

    public MethodSymbol Symbol { get; } = symbol;
    public TypeSymbol ParentType { get; } = parentType;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TypeSymbol ReturnType { get; } = returnType;
    public IRBlockStmt Body { get; } = body;
    public ParameterSymbol? ThisParameter { get; } = thisParameter;
}