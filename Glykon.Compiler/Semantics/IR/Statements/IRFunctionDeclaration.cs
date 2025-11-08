using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRFunctionDeclaration(FunctionSymbol signature, ParameterSymbol[] parameters, TypeSymbol returnType, IRBlockStmt body) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Function;
    public FunctionSymbol Signature { get;} = signature;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TypeSymbol ReturnType { get; } = returnType;
    public IRBlockStmt Body { get; } = body;
}
