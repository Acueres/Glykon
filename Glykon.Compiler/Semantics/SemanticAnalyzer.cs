using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics;

public class SemanticAnalyzer(SyntaxTree syntaxTree, SymbolTable symbolTable, string fileName)
{
    readonly SyntaxTree syntaxTree = syntaxTree;

    readonly TypeChecker typeChecker = new(symbolTable, fileName);
    readonly SemanticRefiner refiner = new(symbolTable, fileName);

    public List<IGlykonError> Execute()
    {
        symbolTable.ResetScope();

        foreach (var statement in syntaxTree)
        {
            typeChecker.Analyze(statement);
        }

        symbolTable.ResetScope();
        foreach (var statement in syntaxTree)
        {
            refiner.Refine(statement);
        }

        var typeErrors = typeChecker.GetErrors();
        var refinerErrors = refiner.GetErrors();

        var errors = typeErrors.Concat(refinerErrors);
        return [.. errors];
    }
}
