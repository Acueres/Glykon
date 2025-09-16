using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Flow;

public class FlowAnalyzer(BoundTree boundTree, string fileName)
{
    readonly BoundTree boundTree = boundTree;
    readonly List<IGlykonError> errors = [];

    public void AnalyzeFlow()
    {
        foreach (var stmt in boundTree)
        {
            CheckUnenclosedJumpStatements(stmt);
        }
    }

    public List<IGlykonError> GetErrors() => errors;
    void CheckUnenclosedJumpStatements(BoundStatement statement)
    {
        if (statement.Kind == StatementKind.While) return;

        if (statement is BoundIfStmt ifStmt)
        {
            CheckUnenclosedJumpStatements(ifStmt.ThenStatement);

            if (ifStmt.ElseStatement is not null)
            {
                CheckUnenclosedJumpStatements(ifStmt.ElseStatement);
            }
            return;
        }

        if (statement is BoundBlockStmt blockStmt)
        {
            foreach (BoundStatement s in blockStmt.Statements)
            {
                CheckUnenclosedJumpStatements(s);
            }

            return;
        }

        if (statement is BoundJumpStmt jumpStmt)
        {
            TypeError error = new(fileName, "No enclosing loop out of which to break or continue");
            errors.Add(error);
        }
    }
}
