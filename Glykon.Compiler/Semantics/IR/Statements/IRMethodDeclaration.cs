using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRMethodDeclaration(
    MethodSymbol signature,
    TypeSymbol parentType,
    ParameterSymbol[] parameters,
    TypeSymbol returnType,
    IRBlockStmt body,
    bool isStatic) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Method;

    public MethodSymbol Signature { get; } = signature;
    public TypeSymbol ParentType { get; } = parentType;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TypeSymbol ReturnType { get; } = returnType;
    public IRBlockStmt Body { get; } = body;
    public bool IsStatic { get; } = isStatic;
}