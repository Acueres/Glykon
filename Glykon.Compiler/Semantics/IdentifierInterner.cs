namespace Glykon.Compiler.Semantics;

public class IdentifierInterner
{
    private readonly Dictionary<string, int> toId = new(StringComparer.Ordinal);
    private readonly List<string> fromId = [];

    public int Intern(string name)
    {
        if (toId.TryGetValue(name, out var id))
            return id;

        id = fromId.Count;
        fromId.Add(name);
        toId[name] = id;
        return id;
    }

    public bool TryGetId(string name, out int id) => toId.TryGetValue(name, out id);
    public string this[int id] => fromId[id];
}
