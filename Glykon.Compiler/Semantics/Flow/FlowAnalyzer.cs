using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;

namespace Glykon.Compiler.Semantics.Flow;

public class FlowAnalyzer(BoundTree boundTree, string fileName)
{
    private readonly List<IGlykonError> errors = [];

    public List<IGlykonError> Analyze()
    {
        foreach (var stmt in boundTree)
        {
            Visit(stmt, inContainer: false, loopDepth: 0);
        }

        return errors;
    }

    private void Visit(BoundStatement s, bool inContainer, int loopDepth)
    {
        while (true)
        {
            switch (s)
            {
                case BoundBlockStmt b:
                    foreach (var child in b.Statements) Visit(child, inContainer, loopDepth);
                    break;

                case BoundIfStmt iff:
                    Visit(iff.ThenStatement, inContainer, loopDepth);
                    if (iff.ElseStatement is not null)
                    {
                        s = iff.ElseStatement;
                        continue;
                    }

                    break;

                case BoundWhileStmt w:
                    s = w.Body;
                    loopDepth += 1;
                    continue;

                case BoundForStmt @for:
                    s = @for.Body;
                    loopDepth += 1;
                    continue;

                case BoundFunctionDeclaration f:
                    s = f.Body;
                    inContainer = true;
                    loopDepth = 0;
                    continue;

                case BoundMethodDeclaration m:
                    s = m.Body;
                    inContainer = true;
                    loopDepth = 0;
                    continue;

                case BoundReturnStmt r:
                    if (!inContainer)
                    {
                        errors.Add(new FlowError(fileName, "Return statement outside of a container", r.Token));
                    }

                    break;

                case BoundJumpStmt j:
                    if (loopDepth == 0)
                        errors.Add(new FlowError(fileName,
                            $"No enclosing loop out of which to {(j.IsBreak ? "break" : "continue")}", j.Token));
                    break;
            }

            break;
        }
    }
}