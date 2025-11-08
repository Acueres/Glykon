using System.Collections;
using Glykon.Compiler.Semantics.IR.Statements;

namespace Glykon.Compiler.Semantics.IR;

public class IRTree(IRStatement[] statements, string fileName) : IEnumerable<IRStatement>
{
    public readonly string FileName = fileName;

    readonly IRStatement[] statements = statements;

    public IRStatement this[int index]
    {
        get => statements[index];
        set => statements[index] = value;
    }

    public int Length => statements.Length;

    public IEnumerator<IRStatement> GetEnumerator()
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
