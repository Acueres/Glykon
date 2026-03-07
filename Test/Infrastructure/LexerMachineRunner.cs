using System.Text.RegularExpressions;

using Glykon.Compiler.Syntax;

namespace Tests.Infrastructure;

public static class LexerMachineRunner
{
    public static bool Accepts(LexerMachineDto m, string input)
    {
        var accepting = m.States.ToDictionary(s => s.Id, s => s.Accepting);
        var transitions = m.Transitions.GroupBy(t => t.FromStateId)
            .ToDictionary(g => g.Key, g => g.ToArray());

        int state = m.StartStateId;

        foreach (char c in input)
        {
            if (!transitions.TryGetValue(state, out var outs))
                return false;

            int? next = null;
            foreach (var t in outs)
            {
                if (Matches(t.Predicate, c))
                {
                    next = t.ToStateId;
                    break; // first-match priority by order
                }
            }

            if (next is null)
                return false;

            state = next.Value;
        }

        return accepting.TryGetValue(state, out var ok) && ok;
    }

    private static bool Matches(string charClass, char c)
    {
        if (charClass == "*") return true;

        if (charClass.StartsWith("lit:", StringComparison.Ordinal))
        {
            var lit = charClass["lit:".Length..];
            // expecting a single-char literal, but we’ll accept if it’s exactly the same string
            return lit.Length == 1 && lit[0] == c;
        }

        if (charClass.StartsWith("re:", StringComparison.Ordinal))
        {
            var pattern = charClass["re:".Length..];
            return Regex.IsMatch(c.ToString(), pattern);
        }

        throw new InvalidOperationException($"Unknown predicate: {charClass}");
    }
}