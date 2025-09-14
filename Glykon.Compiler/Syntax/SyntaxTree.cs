using System.Collections;

using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Syntax;

public class SyntaxTree(Statement[] statements, string fileName) : IEnumerable<Statement>
{
    public readonly string FileName = fileName;

    readonly Statement[] statements = statements;

    public Statement this[int index]
    {
        get => statements[index];
        set => statements[index] = value;
    }

    public int Length => statements.Length;

    public IEnumerator<Statement> GetEnumerator()
    {
        foreach (var statement in statements)
        {
            yield return statement;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
       return GetEnumerator();
    }
}