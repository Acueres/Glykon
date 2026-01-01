using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundMethodDeclaration(
    MethodSymbol symbol,
    TypeSymbol parentType,
    ParameterSymbol[] parameters,
    TypeSymbol returnType,
    BoundBlockStmt body,
    bool isStatic) : BoundStatement
{
    public override BoundStatementKind Kind => BoundStatementKind.Method;

    public MethodSymbol Symbol { get; } = symbol;
    public TypeSymbol ParentType { get; } = parentType;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TypeSymbol ReturnType { get; } = returnType;
    public BoundBlockStmt Body { get; } = body;
    public bool IsStatic { get; } = isStatic;
}