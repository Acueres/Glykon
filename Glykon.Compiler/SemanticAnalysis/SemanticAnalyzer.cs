using Glykon.Compiler.Tokenization;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.SemanticAnalysis;

public class SemanticAnalyzer(IStatement[] statements, SymbolTable symbolTable, string fileName)
{
    readonly IStatement[] statements = statements;

    readonly TypeChecker typeChecker = new(symbolTable, fileName);
    readonly SemanticRefiner refiner = new(symbolTable, fileName);

    public List<IGlykonError> Execute()
    {
        symbolTable.ResetScope();

        foreach (var statement in statements)
        {
            typeChecker.Analyze(statement);
        }

        symbolTable.ResetScope();
        foreach (var statement in statements)
        {
            refiner.Refine(statement);
        }

        var typeErrors = typeChecker.GetErrors();
        var refinerErrors = refiner.GetErrors();

        var errors = typeErrors.Concat(refinerErrors);
        return [.. errors];
    }
}
