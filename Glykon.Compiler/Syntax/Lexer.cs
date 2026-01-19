using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Compiler.Syntax;

public class Lexer(SourceText source, string fileName, SyntaxMode mode)
{
    private bool AtEnd => currentCharIndex >= source.Length;

    private readonly List<Token> tokens = [];
    private readonly List<IGlykonError> errors = [];
    private int line;
    private int currentCharIndex;
    
    private static readonly HashSet<char> whitespaceChars = BuildSingleCharSet(LanguageSpec.Trivia.WhitespaceChars);
    private static readonly HashSet<char> quoteChars = BuildSingleCharSet(LanguageSpec.Strings.QuoteChars);
    private static readonly char newlineChar = LanguageSpec.Trivia.Newline;
    private static readonly char lineCommentStart = LanguageSpec.Trivia.LineCommentStart;
    private static readonly Dictionary<char, LanguageSpec.FixedTokenSpec[]> fixedByFirstChar = BuildFixedByFirstChar();
    
    public LexResult Lex()
    {
        while (!AtEnd)
        {
            Token token = GetNextToken();
            if (!token.IsEmpty)
            {
                tokens.Add(token);
            }
        }

        if (tokens.Count > 0)
        {
            if (mode == SyntaxMode.Predict)
            {
                tokens.Add(new Token(TokenKind.Cursor, line));
            }

            tokens.Add(new Token(TokenKind.EOF, line));
        }

        var processedTokens = InsertTerminators();

        return new LexResult(processedTokens, [..errors]);
    }

    private Token GetNextToken()
    {
        char c = Advance();

        if (whitespaceChars.Contains(c))
        {
            return Token.Empty;
        }

        if (c == newlineChar)
        {
            return new Token(LanguageSpec.Asi.NewlineTokenKind, line++);
        }
        
        if (c == lineCommentStart)
        {
            while (!AtEnd && Peek() != newlineChar) Advance();
            return Token.Empty;
        }

        if (quoteChars.Contains(c))
        {
            return ScanString(c);
        }

        if (char.IsAsciiDigit(c))
        {
            return ScanNumber();
        }

        if (c == '.' && char.IsAsciiDigit(Peek()))
        {
            return ScanNumber(isReal: true);
        }

        if (LanguageSpec.IsAllowedIdentifierStartCharacter(c))
        {
            return ScanIdentifier();
        }

        if (TryScanFixedToken(c, out var kind))
        {
            return new Token(kind, line);
        }
        
        errors.Add(new SyntaxError(line, fileName, $"Invalid character '{c}' in token"));
        return Token.Empty;
    }

    private Token ScanIdentifier()
    {
        int identifierStart = currentCharIndex - 1;

        while (LanguageSpec.IsAllowedIdentifierCharacter(Peek())) Advance();

        TextSpan identifier = new(source, identifierStart, currentCharIndex - identifierStart);

        if (LanguageSpec.TryGetKeywordKind(identifier.Text, out TokenKind type))
        {
            return new Token(type, line);
        }

        return new Token(TokenKind.Identifier, line, identifier);
    }

    private Token ScanNumber(bool isReal = false)
    {
        int numberStart = currentCharIndex - 1;

        while (char.IsAsciiDigit(Peek())) Advance();

        if (Peek() == '.')
        {
            bool canTakeDot =
                LanguageSpec.Numerics.AllowTrailingDotReal ||
                (!LanguageSpec.Numerics.RequireDigitAfterDotIfHasLeadingDigits || char.IsAsciiDigit(Peek(1)));
                
            if (canTakeDot && char.IsAsciiDigit(Peek(1)))
            {
                isReal = true;
                Advance();
                while (char.IsAsciiDigit(Peek())) Advance();
            }
        }

        TextSpan number = new(source, numberStart, currentCharIndex - numberStart);

        TokenKind type = isReal
            ? LanguageSpec.Numerics.RealTokenKind
            : LanguageSpec.Numerics.IntegerTokenKind;

        return new Token(type, line, number);
    }

    private Token ScanString(char openingQuote)
    {
        bool multiline = Match(openingQuote, 2);
        int currentLine = line;
        int stringStart = currentCharIndex;

        while (!AtEnd && !(multiline ? Match(openingQuote, 3) : Match(openingQuote)))
        {
            if (Peek() == newlineChar)
            {
                if (!multiline)
                {
                    errors.Add(new SyntaxError(line, fileName, "Unterminated string literal"));
                    return Token.Empty;
                }

                line++;
            }

            Advance();
        }

        if (AtEnd)
        {
            errors.Add(new SyntaxError(line, fileName, "Unterminated string literal"));
            return Token.Empty;
        }

        int stringEndOffset = multiline ? 3 : 1;
        Token result = new(TokenKind.LiteralString, currentLine,
            new TextSpan(source, stringStart, currentCharIndex - stringStart - stringEndOffset));

        return result;
    }

    private Token[] InsertTerminators()
    {
        List<Token> processedTokens = [];
        
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind != LanguageSpec.Asi.NewlineTokenKind)
            {
                processedTokens.Add(token);
                continue;
            }
            
            if (i == 0) continue;
            
            var previousToken = tokens[i - 1];
            
            if (LanguageSpec.AsiNoInsertAfter(previousToken.Kind)) continue;

            if (i == tokens.Count - 1) continue;
            
            var nextToken = tokens[i + 1];
            
            if (LanguageSpec.AsiContinuationBefore(nextToken.Kind)) continue;
            
            processedTokens.Add(new Token(LanguageSpec.Asi.VirtualTerminatorKind, token.Line));
        }
        
        return processedTokens.ToArray();
    }
    
    private bool TryScanFixedToken(char firstChar, out TokenKind kind)
    {
        if (!fixedByFirstChar.TryGetValue(firstChar, out var candidates))
        {
            kind = default;
            return false;
        }

        for (int i = 0; i < candidates.Length; i++)
        {
            var fx = candidates[i];
            if (MatchRemainder(fx.Spelling))
            {
                kind = fx.Kind;
                return true;
            }
        }

        kind = default;
        return false;
    }

    private bool MatchRemainder(string spelling)
    {
        // spelling[0] was already consumed
        for (int i = 1; i < spelling.Length; i++)
        {
            if (Peek(i - 1) != spelling[i])
            {
                return false;
            }
        }

        currentCharIndex += spelling.Length - 1;
        return true;
    }

    private bool Match(char token)
    {
        if (AtEnd || Peek() != token) return false;
        currentCharIndex++;
        return true;
    }

    private bool Match(char token, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            if (AtEnd || Peek(i) != token) return false;
        }

        currentCharIndex += offset;

        return true;
    }

    private char Peek(int offset = 0)
    {
        int nextCharPos = currentCharIndex + offset;
        char c = AtEnd || nextCharPos >= source.Length ? '\0' : source[nextCharPos];
        return c;
    }

    private char Advance()
    {
        return source[currentCharIndex++];
    }
    
    private static HashSet<char> BuildSingleCharSet(string[] items)
    {
        var set = new HashSet<char>();
        for (int i = 0; i < items.Length; i++)
        {
            var s = items[i];
            if (!string.IsNullOrEmpty(s))
            {
                set.Add(s[0]);
            }
        }
        return set;
    }
    
    private static Dictionary<char, LanguageSpec.FixedTokenSpec[]> BuildFixedByFirstChar()
    {
        var dict = new Dictionary<char, List<LanguageSpec.FixedTokenSpec>>();

        var newlineKind = LanguageSpec.Asi.NewlineTokenKind;

        foreach (var fx in LanguageSpec.FixedTokenEntries)
        {
            if (string.IsNullOrEmpty(fx.Spelling))
            {
                continue;
            }

            if (fx.Kind == newlineKind)
            {
                continue;
            }

            char first = fx.Spelling[0];
            if (!dict.TryGetValue(first, out var list))
            {
                list = [];
                dict[first] = list;
            }

            list.Add(fx);
        }
        
        var result = new Dictionary<char, LanguageSpec.FixedTokenSpec[]>();
        foreach (var (ch, list) in dict)
        {
            list.Sort(static (a, b) =>
            {
                int len = b.Spelling.Length.CompareTo(a.Spelling.Length);
                if (len != 0) return len;
                return string.CompareOrdinal(a.Spelling, b.Spelling);
            });

            result[ch] = list.ToArray();
        }

        return result;
    }
}
