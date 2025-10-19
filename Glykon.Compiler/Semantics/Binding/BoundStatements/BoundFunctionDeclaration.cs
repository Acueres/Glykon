using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundFunctionDeclaration(FunctionSymbol signature, ParameterSymbol[] parameters, TypeSymbol returnType, BoundBlockStmt body) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Function;
    public FunctionSymbol Signature { get;} = signature;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TypeSymbol ReturnType { get; } = returnType;
    public BoundBlockStmt Body { get; } = body;
}
