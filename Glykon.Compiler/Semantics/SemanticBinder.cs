using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics;

public class SemanticBinder(SyntaxTree syntaxTree, IdentifierInterner interner)
{
    readonly SyntaxTree syntaxTree = syntaxTree;
    readonly SymbolTable symbolTable = new(interner);

    public SymbolTable Bind()
    {
        RegisterStd();

        foreach (var stmt in syntaxTree)
        {
            BindStatement(stmt);
        }

        return symbolTable;
    }

    void BindStatement(IStatement stmt)
    {
        if (stmt is null) return;

        switch (stmt.Type)
        {
            case StatementType.Block:
                {
                    Scope scope = symbolTable.BeginScope(ScopeKind.Block);
                    var blockStmt = (BlockStmt)stmt;

                    foreach (var statement in blockStmt.Statements)
                    {
                        BindStatement(statement);
                    }

                    blockStmt.Scope = scope;

                    symbolTable.ExitScope();
                    break;
                }
            case StatementType.If:
                {
                    var ifStmt = (IfStmt)stmt;
                    BindStatement(ifStmt.ThenStatement);
                    BindStatement(ifStmt.ElseStatement);
                    break;
                }
            case StatementType.While:
                {
                    var whileStmt = (WhileStmt)stmt;
                    BindStatement(whileStmt.Statement);
                    break;
                }
            case StatementType.Variable:
                {
                    var variableStmt = (VariableStmt)stmt;
                    symbolTable.RegisterVariable(variableStmt.Name, variableStmt.VariableType);
                    break;
                }
            case StatementType.Constant:
                {
                    var constantStmt = (ConstantStmt)stmt;
                    symbolTable.RegisterConstant(constantStmt.Name, constantStmt.Token, constantStmt.ConstantType);
                    break;
                }

            case StatementType.Function:
                {
                    var functionStmt = (FunctionStmt)stmt;

                    var symbol = symbolTable.RegisterFunction(functionStmt.Name, functionStmt.ReturnType, [.. functionStmt.Parameters.Select(p => p.Type)]);
                    var scope = symbolTable.BeginScope(symbol);

                    List<ParameterSymbol> parameterSymbols = new(functionStmt.Parameters.Count);
                    foreach (var parameter in functionStmt.Parameters)
                    {
                        var parameterSymbol = symbolTable.RegisterParameter(parameter.Name, parameter.Type);
                        parameterSymbols.Add(parameterSymbol);
                    }

                    functionStmt.Signature = symbol;
                    functionStmt.ParameterSymbols = parameterSymbols;

                    foreach (var statement in functionStmt.Body.Statements)
                    {
                        BindStatement(statement);
                    }

                    functionStmt.Body.Scope = scope;
                    symbolTable.ExitScope();

                    break;
                }
        }
    }

    void RegisterStd()
    {
        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.String]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Int]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Real]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Bool]);
    }
}
