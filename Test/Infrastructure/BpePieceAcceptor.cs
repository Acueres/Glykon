using System.Text.RegularExpressions;

using Glykon.Compiler.Syntax;

namespace Tests.Infrastructure;

public static class BpePieceAcceptor
    {
        public sealed record Context(IReadOnlyList<Progress> Open)
        {
            public static readonly Context Empty = new([]);
            public bool IsBoundary => Open.Count == 0;
        }

        public abstract record Progress(TokenKind Kind);

        private sealed record FixedProgress(TokenKind Kind, string Spelling, int Pos) : Progress(Kind)
        {
            public bool IsAccepting => Pos == Spelling.Length;
            public bool IsPunctLike => !IsIdentChar(Spelling[0]); // heuristic
            public bool IsKeywordLike => IsIdentChar(Spelling[0]); // heuristic
        }

        private sealed record MachineProgress(TokenKind Kind, LexerMachineDto Machine, int State) : Progress(Kind);

        public static bool TryAdvance(
            Context ctx,
            IReadOnlyList<TokenKind> expected,
            string piece,
            IReadOnlyDictionary<TokenKind, string> fixedSpellings,
            IReadOnlyDictionary<TokenKind, LexerMachineDto> patternMachines,
            out Context next)
        {
            next = ctx;

            // Split: leading trivia + core + trailing trivia
            SplitTrivia(piece, out var leadingTrivia, out var core, out var trailingTrivia);

            // leading trivia can close an open token only if accepting
            if (leadingTrivia.Length > 0)
            {
                if (!ctx.IsBoundary && !AnyAccepting(ctx.Open))
                    return false;

                // close token at trivia boundary
                ctx = Context.Empty;
            }

            // trivia-only piece
            if (core.Length == 0)
            {
                // trailing trivia is also fine; result is boundary
                next = Context.Empty;
                return true;
            }

            // consume core
            IReadOnlyList<Progress> openAfterCore;

            if (ctx.IsBoundary)
            {
                openAfterCore = StartFromExpected(expected, core, fixedSpellings, patternMachines);
                if (openAfterCore.Count == 0) return false;
            }
            else
            {
                // try continue
                var continued = ContinueOpen(ctx.Open, core);
                if (continued.Count > 0)
                {
                    openAfterCore = continued;
                }
                else
                {
                    // continuation failed; allow delimiter-restart ONLY if the first char is a delimiter
                    // for at least one accepting alternative.
                    if (!AnyAccepting(ctx.Open))
                        return false;

                    char first = core[0];
                    if (!AnyCanDelimitHere(ctx.Open, first))
                        return false;

                    // close and restart as a new token at this piece boundary
                    openAfterCore = StartFromExpected(expected, core, fixedSpellings, patternMachines);
                    if (openAfterCore.Count == 0) return false;
                }
            }

            // If all remaining paths are accepting and punct-like, auto-close (no delimiter required).
            if (AllAcceptingAndPunctLike(openAfterCore))
            {
                ctx = Context.Empty;
            }
            else
            {
                ctx = new Context(openAfterCore);
            }

            // trailing trivia: may close only if accepting
            if (trailingTrivia.Length > 0)
            {
                if (ctx.IsBoundary)
                {
                    next = ctx;
                    return true;
                }

                if (!AnyAccepting(ctx.Open))
                    return false;

                // close at trivia
                next = Context.Empty;
                return true;
            }

            next = ctx;
            return true;
        }

        private static void SplitTrivia(string piece, out string leading, out string core, out string trailing)
        {
            int i = 0;
            while (i < piece.Length && IsTrivia(piece[i])) i++;

            int j = piece.Length - 1;
            while (j >= i && IsTrivia(piece[j])) j--;

            leading = piece[..i];
            core = i <= j ? piece[i..(j + 1)] : "";
            trailing = j + 1 < piece.Length ? piece[(j + 1)..] : "";
        }

        private static bool IsTrivia(char c) => c is ' ' or '\t' or '\r' or '\n';

        private static bool IsIdentChar(char c) =>
            c == '_' || c == '@' || char.IsLetterOrDigit(c);

        private static bool AnyAccepting(IReadOnlyList<Progress> open)
        {
            foreach (var p in open)
            {
                switch (p)
                {
                    case FixedProgress fp when fp.IsAccepting:
                        return true;
                    case MachineProgress mp when IsAccepting(mp.Machine, mp.State):
                        return true;
                }
            }
            return false;
        }

        private static bool AnyCanDelimitHere(IReadOnlyList<Progress> open, char nextChar)
        {
            foreach (var p in open)
            {
                switch (p)
                {
                    case FixedProgress fp when fp.IsAccepting:
                        // keyword-like fixed tokens need a non-ident char delimiter
                        if (fp.IsKeywordLike)
                        {
                            if (!IsIdentChar(nextChar)) return true;
                        }
                        else
                        {
                            // punct-like fixed tokens delimit before any next char
                            return true;
                        }
                        break;

                    case MachineProgress mp when IsAccepting(mp.Machine, mp.State):
                        // delimiter if machine cannot continue with nextChar
                        if (!TryAdvanceMachine(mp.Machine, mp.State, nextChar.ToString(), out _))
                            return true;
                        break;
                }
            }
            return false;
        }

        private static bool AllAcceptingAndPunctLike(IReadOnlyList<Progress> open)
        {
            if (open.Count == 0) return false;

            foreach (var p in open)
            {
                if (p is not FixedProgress fp) return false;
                if (!fp.IsAccepting) return false;
                if (!fp.IsPunctLike) return false;
            }
            return true;
        }

        private static List<Progress> StartFromExpected(
            IReadOnlyList<TokenKind> expected,
            string core,
            IReadOnlyDictionary<TokenKind, string> fixedSpellings,
            IReadOnlyDictionary<TokenKind, LexerMachineDto> patternMachines)
        {
            var res = new List<Progress>();

            foreach (var k in expected)
            {
                if (fixedSpellings.TryGetValue(k, out var spelling))
                {
                    if (spelling.StartsWith(core, StringComparison.Ordinal))
                    {
                        res.Add(new FixedProgress(k, spelling, core.Length));
                    }
                }
                else if (patternMachines.TryGetValue(k, out var m))
                {
                    if (TryAdvanceMachine(m, m.StartStateId, core, out var newState))
                    {
                        res.Add(new MachineProgress(k, m, newState));
                    }
                }
            }

            return res;
        }

        private static List<Progress> ContinueOpen(IReadOnlyList<Progress> open, string core)
        {
            var res = new List<Progress>();

            foreach (var p in open)
            {
                switch (p)
                {
                    case FixedProgress fp:
                    {
                        if (fp.Pos > fp.Spelling.Length) break;
                        var remaining = fp.Spelling.AsSpan(fp.Pos);
                        if (remaining.StartsWith(core, StringComparison.Ordinal))
                        {
                            res.Add(fp with { Pos = fp.Pos + core.Length });
                        }
                        break;
                    }
                    case MachineProgress mp:
                    {
                        if (TryAdvanceMachine(mp.Machine, mp.State, core, out var newState))
                        {
                            res.Add(mp with { State = newState });
                        }
                        break;
                    }
                }
            }

            return res;
        }

        private static bool IsAccepting(LexerMachineDto m, int stateId)
        {
            foreach (var s in m.States)
            {
                if (s.Id == stateId) return s.Accepting;
            }
            return false;
        }

        private static bool TryAdvanceMachine(LexerMachineDto m, int state, string chunk, out int nextState)
        {
            nextState = state;

            // group transitions by from
            var grouped = m.Transitions
                .GroupBy(t => t.FromStateId)
                .ToDictionary(g => g.Key, g => g.ToArray());

            int cur = state;

            foreach (char c in chunk)
            {
                if (!grouped.TryGetValue(cur, out var outs))
                    return false;

                int? nxt = null;
                foreach (var t in outs)
                {
                    if (Matches(t.Predicate, c))
                    {
                        nxt = t.ToStateId;
                        break;
                    }
                }

                if (nxt is null) return false;
                cur = nxt.Value;
            }

            nextState = cur;
            return true;
        }

        private static bool Matches(string predicate, char c)
        {
            if (predicate == "*") return true;

            if (predicate.StartsWith("lit:", StringComparison.Ordinal))
            {
                var lit = predicate["lit:".Length..];
                return lit.Length == 1 && lit[0] == c;
            }

            if (predicate.StartsWith("re:", StringComparison.Ordinal))
            {
                var pattern = predicate["re:".Length..];
                return Regex.IsMatch(c.ToString(), pattern);
            }

            throw new InvalidOperationException($"Unknown predicate: {predicate}");
        }
    }