using System.Collections;

using Glykon.Compiler.Semantics.Binding.BoundStatements;

namespace Glykon.Compiler.Semantics.Binding;

public class BoundTree(BoundStatement[] statements, string fileName) : IEnumerable<BoundStatement>
{
    public readonly string FileName = fileName;

    readonly BoundStatement[] statements = statements;

    public BoundStatement this[int index]
    {
        get => statements[index];
        set => statements[index] = value;
    }

    public int Length => statements.Length;

    public IEnumerator<BoundStatement> GetEnumerator()
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
