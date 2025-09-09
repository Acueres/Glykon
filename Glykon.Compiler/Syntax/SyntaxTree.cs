using System.Collections;

using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Syntax;

public class SyntaxTree(IStatement[] statements, string fileName) : IEnumerable<IStatement>
{
    public readonly string FileName = fileName;

    readonly IStatement[] statements = statements;

    public IStatement this[int index]
    {
        get => statements[index];
        set => statements[index] = value;
    }

    public int Length => statements.Length;

    public IEnumerator<IStatement> GetEnumerator()
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