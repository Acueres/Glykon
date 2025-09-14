using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding.BoundStatements;

public class BoundFunctionDeclaration(string name, FunctionSymbol signature, ParameterSymbol[] parameters, TokenKind returnType, BoundBlockStmt body) : BoundStatement
{
    public override StatementKind Kind => StatementKind.Function;
    public string Name { get; } = name;
    public FunctionSymbol Signature { get;} = signature;
    public ParameterSymbol[] Parameters { get; } = parameters;
    public TokenKind ReturnType { get; } = returnType;
    public BoundBlockStmt Body { get; } = body;
}
