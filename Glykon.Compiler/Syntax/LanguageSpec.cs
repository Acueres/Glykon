using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Glykon.Compiler.Syntax;

/// <summary>
/// Glykon lexical rules.
///
/// This spec is designed to be machine-readable (DTO + deterministic JSON)
/// 
/// NOTE: The hash is computed over the canonical JSON WITHOUT the "specHash" field
/// (i.e., over <see cref="ToDtoWithoutHash"/>). This avoids self-referential hashing.
/// </summary>
public static class LanguageSpec
{
    // -------------------------
    // Versioning / hashing
    // -------------------------

    /// <summary>
    /// Manual spec version. Increment when any exported rule/table changes.
    /// </summary>
    public const string SpecVersion = "0.1.0";

    /// <summary>
    /// Deterministic SHA-256 (hex, lowercase) of canonical JSON (excluding specHash field).
    /// </summary>
    public static readonly string SpecHash;

    // -------------------------
    // Canonical JSON options
    // -------------------------

    /// <summary>
    /// Canonical options used for hashing and deterministic JSON output.
    /// </summary>
    public static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // -------------------------
    // Keywords
    // -------------------------

    /// <summary>
    /// Keyword entries (canonical order is by keywordText ordinal).
    /// </summary>
    public static readonly KeywordEntrySpec[] KeywordEntries =
    [
        new("and",
            TokenKind.And),
        new("as",
            TokenKind.As),
        new("break",
            TokenKind.Break),
        new("by",
            TokenKind.By),
        new("class",
            TokenKind.Class),
        new("const",
            TokenKind.Const),
        new("continue",
            TokenKind.Continue),
        new("def",
            TokenKind.Def),
        new("elif",
            TokenKind.Elif),
        new("else",
            TokenKind.Else),
        new("enum",
            TokenKind.Enum),
        new("false",
            TokenKind.LiteralFalse),
        new("for",
            TokenKind.For),
        new("if",
            TokenKind.If),
        new("in",
            TokenKind.In),
        new("interface",
            TokenKind.Interface),
        new("let",
            TokenKind.Let),
        new("new",
            TokenKind.New),
        new("none",
            TokenKind.None),
        new("not",
            TokenKind.Not),
        new("or",
            TokenKind.Or),
        new("return",
            TokenKind.Return),
        new("struct",
            TokenKind.Struct),
        new("true",
            TokenKind.LiteralTrue),
        new("while",
            TokenKind.While),
    ];

    /// <summary>
    /// Keyword lookup map (exact match, case-sensitive, ordinal).
    /// </summary>
    public static readonly IReadOnlyDictionary<string, TokenKind> Keywords;

    // -------------------------
    // Fixed tokens (operators / punctuators)
    // -------------------------

    /// <summary>
    /// Fixed spelling tokens (canonical order: by token kind id, then spelling ordinal).
    /// </summary>
    public static readonly FixedTokenSpec[] FixedTokenEntries =
    [
        // Symbols
        new(TokenKind.BraceLeft,
            "{"),
        new(TokenKind.BraceRight,
            "}"),
        new(TokenKind.ParenthesisLeft,
            "("),
        new(TokenKind.ParenthesisRight,
            ")"),
        new(TokenKind.BracketLeft,
            "["),
        new(TokenKind.BracketRight,
            "]"),
        new(TokenKind.Comma,
            ","),
        new(TokenKind.Colon,
            ":"),
        new(TokenKind.Dot,
            "."),
        new(TokenKind.Semicolon,
            ";"),

        // Operators (punctuator-like)
        new(TokenKind.Plus,
            "+"),
        new(TokenKind.Minus,
            "-"),
        new(TokenKind.Arrow,
            "->"),

        new(TokenKind.Star,
            "*"),
        new(TokenKind.StarDouble,
            "**"),

        new(TokenKind.Slash,
            "/"),
        new(TokenKind.SlashDouble,
            "//"),

        new(TokenKind.Assignment,
            "="),
        new(TokenKind.Equal,
            "=="),
        new(TokenKind.NotEqual,
            "!="),

        new(TokenKind.Greater,
            ">"),
        new(TokenKind.GreaterEqual,
            ">="),
        new(TokenKind.Less,
            "<"),
        new(TokenKind.LessEqual,
            "<="),

        new(TokenKind.Range,
            ".."),
        new(TokenKind.RangeInclusive,
            "..="),

        // Newline is lexed, then removed during ASI processing.
        new(TokenKind.Newline,
            "\n"),
    ];

    /// <summary>
    /// Fast lookup from TokenKind to spellings (may contain multiple spellings).
    /// </summary>
    public static readonly IReadOnlyDictionary<TokenKind, string[]> FixedSpellingsByKind;

    // -------------------------
    // Identifier rules
    // -------------------------

    public static readonly IdentifierSpec Identifier = new(
        // Start: char.IsLetter(c) || c == '_' || c == '@'
        Start: new IdentifierCharSetSpec(
            IncludeUnicodeLetter: true,
            IncludeAsciiUnderscore: true,
            IncludeAtSign: true,
            IncludeUnicodeLetterOrDigit: false
        ),
        // Continue: c == '_' || char.IsLetterOrDigit(c)
        Continue: new IdentifierCharSetSpec(
            IncludeUnicodeLetter: true,
            IncludeAsciiUnderscore: true,
            IncludeAtSign: false,
            IncludeUnicodeLetterOrDigit: true
        ),
        KeywordsAreReserved: true
    );

    // -------------------------
    // Numerical rules
    // -------------------------

    public static readonly NumberSpec Numerics = new(
        Digits: DigitSet.Ascii,
        // Real number is produced if:
        //  - starts with '.' AND next is digit (handled by caller)
        //  - OR has digits then '.' then digit
        AllowLeadingDotReal: true,
        RequireDigitAfterDotIfHasLeadingDigits: true,
        AllowTrailingDotReal: false,
        AllowExponent: false,
        AllowUnderscoreSeparators: false,
        IntegerTokenKind: TokenKind.LiteralInt,
        RealTokenKind: TokenKind.LiteralReal
    );

    // -------------------------
    // String rules
    // -------------------------

    public static readonly StringSpec Strings = new(
        QuoteChars:
        [
            "\"",
            "'"
        ],
        TripleQuoteEnabled: true,
        MultiLineRequiresTripleQuote: true,
        EscapeMode: StringEscapeMode.None,
        AllowsNewlineInSingleLineString: false,
        TokenKind: TokenKind.LiteralString
    );

    // -------------------------
    // Trivia rules (whitespace + comments)
    // -------------------------

    public static readonly TriviaSpec Trivia = new(
        // Lexer skips ' ', '\r', '\t' as whitespace
        WhitespaceChars:
        [
            " ",
            "\t",
            "\r"
        ],
        Newline: '\n',
        LineCommentStart: '#',
        LineCommentEndsAtNewline: true
    );

    // -------------------------
    // ASI / virtual semicolons + parser exceptions
    // -------------------------

    public static readonly AsiSpec Asi;

    // -------------------------
    // Synthetic token inventory
    // -------------------------

    public static readonly TokenKind[] SyntheticTokenKinds =
    [
        TokenKind.Empty,
        TokenKind.OptionalTerminator,
        TokenKind.Cursor,
        TokenKind.EOF,
    ];

    // -------------------------
    // Initialization
    // -------------------------

    static LanguageSpec()
    {
        // Build keyword dictionary
        Keywords = KeywordEntries
            .OrderBy(k => k.Text,
                StringComparer.Ordinal)
            .ToDictionary(k => k.Text,
                k => k.Kind,
                StringComparer.Ordinal);

        // Build fixed token spelling map
        FixedSpellingsByKind = FixedTokenEntries
            .GroupBy(x => x.Kind)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Spelling)
                    .OrderBy(s => s,
                        StringComparer.Ordinal)
                    .ToArray()
            );

        // ASI sets
        TokenKind[] noInsertAfter =
        [
            // Internal/special
            TokenKind.EOF,
            // Avoid double terminators
            TokenKind.Semicolon,
            TokenKind.Newline,

            // Openers
            TokenKind.BracketLeft,
            TokenKind.ParenthesisLeft,
            TokenKind.BraceLeft,

            // Block closer
            TokenKind.BraceRight,

            // Punctuation and operators that precede an operand
            TokenKind.Comma,
            TokenKind.Colon,
            TokenKind.Dot,
            TokenKind.Assignment,
            TokenKind.Arrow,
            TokenKind.Range,
            TokenKind.RangeInclusive,

            // Arithmetic operators
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.StarDouble,
            TokenKind.Slash,
            TokenKind.SlashDouble,

            // Comparison operators
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Greater,
            TokenKind.GreaterEqual,
            TokenKind.Less,
            TokenKind.LessEqual,

            // Logical operators
            TokenKind.And,
            TokenKind.Or,
            TokenKind.Not,

            // Declarations
            TokenKind.Def,
            TokenKind.Class,
            TokenKind.Struct,
            TokenKind.Interface,
            TokenKind.Enum,
            TokenKind.Let,
            TokenKind.Const,
            TokenKind.New,

            // Control-flow keywords
            TokenKind.If,
            TokenKind.Else,
            TokenKind.Elif,

            // Other keywords
            TokenKind.For,
            TokenKind.By,
            TokenKind.In,
            TokenKind.While,
            TokenKind.As
        ];

        TokenKind[] continuationBefore =
        [
            TokenKind.Cursor,

            // Postfix / delimiters
            TokenKind.Dot,
            TokenKind.ParenthesisLeft,
            TokenKind.ParenthesisRight,
            TokenKind.BracketLeft,
            TokenKind.BracketRight,

            // Arithmetic operators
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.StarDouble,
            TokenKind.Slash,
            TokenKind.SlashDouble,

            // Assignment / comparison
            TokenKind.Assignment,
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Less,
            TokenKind.LessEqual,
            TokenKind.Greater,
            TokenKind.GreaterEqual,

            // Logical operators
            TokenKind.And,
            TokenKind.Not,
            TokenKind.Or,

            // Keyword operator
            TokenKind.As
        ];

        Asi = new AsiSpec(
            Enabled: true,
            VirtualTerminatorKind: TokenKind.Semicolon,
            NewlineTokenKind: TokenKind.Newline,
            DropsNewlineTokens: true,
            NoInsertAfter: noInsertAfter.OrderBy(k => (int)k)
                .ToArray(),
            ContinuationBefore: continuationBefore.OrderBy(k => (int)k)
                .ToArray(),
            // Parser special-case: initializer expressions may terminate by line break even when lexer did NOT insert a semicolon.
            ParserExceptions: new ParserLayoutExceptionSpec(
                AllowMissingTerminatorAfterInitializerIfNextTokenOnNewLine: true,
                OptionalTerminatorTokenKind: TokenKind.OptionalTerminator
            )
        );

        // Compute hash over canonical JSON WITHOUT specHash field
        SpecHash = ComputeSha256Hex(ToJsonUtf8(includeHash: false));
    }

    // -------------------------
    // Public export API
    // -------------------------

    public static LanguageSpecDto ToDto() =>
        ToDto(includeHash: true);

    public static string ToJson(JsonSerializerOptions? opts = null)
    {
        opts ??= CanonicalJsonOptions;
        return JsonSerializer.Serialize(ToDto(includeHash: true),
            opts);
    }

    public static byte[] ToJsonUtf8(JsonSerializerOptions? opts = null)
    {
        opts ??= CanonicalJsonOptions;
        return JsonSerializer.SerializeToUtf8Bytes(ToDto(includeHash: true),
            opts);
    }

    // -------------------------
    // Shared helpers
    // -------------------------

    public static bool IsAllowedIdentifierStartCharacter(char c) =>
        Identifier.Start.IsMatchStart(c);

    public static bool IsAllowedIdentifierCharacter(char c) =>
        Identifier.Continue.IsMatchContinue(c);

    public static bool TryGetKeywordKind(string text,
        out TokenKind kind) =>
        Keywords.TryGetValue(text,
            out kind);

    public static bool AsiNoInsertAfter(TokenKind previous) =>
        Asi.NoInsertAfterSet.Contains(previous);

    public static bool AsiContinuationBefore(TokenKind next) =>
        Asi.ContinuationBeforeSet.Contains(next);

    // -------------------------
    // Internal DTO construction
    // -------------------------

    private static LanguageSpecDto ToDto(bool includeHash)
    {
        var tokens = BuildTokenInventory();

        var keywords = KeywordEntries
            .OrderBy(k => k.Text,
                StringComparer.Ordinal)
            .Select(k => new KeywordEntryDto(k.Text,
                (int)k.Kind,
                k.Kind.ToString()))
            .ToArray();

        var fixedTokens = FixedTokenEntries
            .OrderBy(x => (int)x.Kind)
            .ThenBy(x => x.Spelling,
                StringComparer.Ordinal)
            .Select(x => new FixedTokenDto((int)x.Kind,
                x.Kind.ToString(),
                x.Spelling))
            .ToArray();

        return new LanguageSpecDto(
            SpecVersion: SpecVersion,
            SpecHash: includeHash
                ? SpecHash
                : null,
            Tokens: tokens,
            Keywords: keywords,
            FixedTokens: fixedTokens,
            Identifier: Identifier.ToDto(),
            Numbers: Numerics.ToDto(),
            Strings: Strings.ToDto(),
            Trivia: Trivia.ToDto(),
            Asi: Asi.ToDto()
        );
    }

    private static LanguageSpecDto ToDtoWithoutHash() =>
        ToDto(includeHash: false);

    private static TokenInfoDto[] BuildTokenInventory()
    {
        var allKinds = Enum.GetValues(typeof(TokenKind))
            .Cast<TokenKind>()
            .OrderBy(k => (int)k)
            .ToArray();

        // Reverse lookup: kind -> keyword spellings
        var keywordSpellings = KeywordEntries
            .GroupBy(k => k.Kind)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Text)
                    .OrderBy(s => s,
                        StringComparer.Ordinal)
                    .ToArray()
            );

        // Define token classification sets
        var synthetic = new HashSet<TokenKind>(SyntheticTokenKinds);
        var literalKinds = new HashSet<TokenKind>
        {
            TokenKind.None,
            TokenKind.LiteralTrue,
            TokenKind.LiteralFalse,
            TokenKind.LiteralInt,
            TokenKind.LiteralReal,
            TokenKind.LiteralString,
        };

        var symbolKinds = new HashSet<TokenKind>
        {
            TokenKind.BracketLeft,
            TokenKind.BracketRight,
            TokenKind.ParenthesisLeft,
            TokenKind.ParenthesisRight,
            TokenKind.BraceLeft,
            TokenKind.BraceRight,
            TokenKind.Comma,
            TokenKind.Colon,
            TokenKind.Dot,
            TokenKind.Semicolon,
            TokenKind.Newline,
        };

        var operatorKinds = new HashSet<TokenKind>
        {
            TokenKind.Minus,
            TokenKind.Plus,
            TokenKind.Slash,
            TokenKind.SlashDouble,
            TokenKind.Star,
            TokenKind.StarDouble,
            TokenKind.Assignment,
            TokenKind.Arrow,
            TokenKind.Range,
            TokenKind.RangeInclusive,
            TokenKind.NotEqual,
            TokenKind.Equal,
            TokenKind.Greater,
            TokenKind.GreaterEqual,
            TokenKind.Less,
            TokenKind.LessEqual,

            // keyword-operators
            TokenKind.And,
            TokenKind.Not,
            TokenKind.Or,
            TokenKind.As,
        };

        bool IsLexable(TokenKind k)
        {
            if (synthetic.Contains(k))
                return false;
            return k switch
            {
                TokenKind.Cursor or TokenKind.OptionalTerminator or TokenKind.Empty or TokenKind.EOF => false,
                _ => true
            };
        }

        var infos = new List<TokenInfoDto>(allKinds.Length);
        foreach (var kind in allKinds)
        {
            var id = (int)kind;
            var name = kind.ToString();

            string category;
            if (synthetic.Contains(kind))
                category = "synthetic";
            else if (literalKinds.Contains(kind))
                category = "literal";
            else if (kind == TokenKind.Identifier)
                category = "pattern";
            else if (symbolKinds.Contains(kind))
                category = "symbol";
            else if (operatorKinds.Contains(kind))
                category = "operator";
            else if (keywordSpellings.ContainsKey(kind))
                category = "keyword";
            else
                category = "unknown";

            // Spellings only for fixed-form tokens (keywords / operators / punctuators)
            // Pattern tokens (Identifier, LiteralInt/Real/String) intentionally have no spellings.
            var spellings = Array.Empty<string>();
            if (kind is not (TokenKind.Identifier or TokenKind.LiteralInt or TokenKind.LiteralReal or TokenKind.LiteralString))
            {
                var merged = new List<string>();
                if (keywordSpellings.TryGetValue(kind,
                        out var kw))
                    merged.AddRange(kw);
                if (FixedSpellingsByKind.TryGetValue(kind,
                        out var fx))
                    merged.AddRange(fx);
                if (merged.Count > 0)
                {
                    spellings = merged.Distinct(StringComparer.Ordinal)
                        .OrderBy(s => s,
                            StringComparer.Ordinal)
                        .ToArray();
                }
            }

            bool isSynthetic = synthetic.Contains(kind);
            bool isTrivia = kind == TokenKind.Newline; // newline is lexed but removed during ASI processing
            bool isVirtual = kind == TokenKind.Semicolon; // may be inserted by ASI

            infos.Add(new TokenInfoDto(
                Id: id,
                Name: name,
                Category: category,
                Spellings: spellings,
                IsLexable: IsLexable(kind),
                IsSynthetic: isSynthetic,
                IsTrivia: isTrivia,
                MayBeVirtual: isVirtual
            ));
        }

        return infos.OrderBy(t => t.Id)
            .ToArray();
    }

    // -------------------------
    // Hashing
    // -------------------------

    private static byte[] ToJsonUtf8(bool includeHash) =>
        JsonSerializer.SerializeToUtf8Bytes(ToDto(includeHash),
            CanonicalJsonOptions);

    private static string ComputeSha256Hex(byte[] data)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(data);
        return Convert.ToHexString(hash)
            .ToLowerInvariant();
    }

    // -------------------------
    // Spec primitives (data)
    // -------------------------

    public readonly record struct KeywordEntrySpec(
        string Text,
        TokenKind Kind);

    public readonly record struct FixedTokenSpec(
        TokenKind Kind,
        string Spelling);
}

// =========================
// DTOs for JSON export
// =========================

public sealed record LanguageSpecDto(
    string SpecVersion,
    string? SpecHash,
    TokenInfoDto[] Tokens,
    KeywordEntryDto[] Keywords,
    FixedTokenDto[] FixedTokens,
    IdentifierSpecDto Identifier,
    NumberSpecDto Numbers,
    StringSpecDto Strings,
    TriviaSpecDto Trivia,
    AsiSpecDto Asi
);

public sealed record TokenInfoDto(
    int Id,
    string Name,
    string Category,
    string[] Spellings,
    bool IsLexable,
    bool IsSynthetic,
    bool IsTrivia,
    bool MayBeVirtual
);

public sealed record KeywordEntryDto(
    string Text,
    int TokenId,
    string TokenName
);

public sealed record FixedTokenDto(
    int TokenId,
    string TokenName,
    string Spelling
);

// =========================
// Identifier
// =========================

public sealed record IdentifierSpec(
    IdentifierCharSetSpec Start,
    IdentifierCharSetSpec Continue,
    bool KeywordsAreReserved
)
{
    public IdentifierSpecDto ToDto() =>
        new(
            Start: Start.ToDto(isStart: true),
            Continue: Continue.ToDto(isStart: false),
            KeywordsAreReserved: KeywordsAreReserved
        );
}

public sealed record IdentifierCharSetSpec(
    bool IncludeUnicodeLetter,
    bool IncludeAsciiUnderscore,
    bool IncludeAtSign,
    bool IncludeUnicodeLetterOrDigit
)
{
    public IdentifierCharSetDto ToDto(bool isStart)
    {
        // Machine-friendly list of categories.
        var cats = new List<string>();
        if (IncludeUnicodeLetter)
            cats.Add("unicodeLetter");
        if (IncludeUnicodeLetterOrDigit && !isStart)
            cats.Add("unicodeLetterOrDigit");
        if (IncludeAsciiUnderscore)
            cats.Add("underscore");
        if (IncludeAtSign)
            cats.Add("atSign");
        return new IdentifierCharSetDto(cats.OrderBy(c => c,
                StringComparer.Ordinal)
            .ToArray());
    }

    public bool IsMatchStart(char c)
    {
        if (IncludeAsciiUnderscore && c == '_')
            return true;
        if (IncludeAtSign && c == '@')
            return true;
        if (IncludeUnicodeLetter && char.IsLetter(c))
            return true;

        return false;
    }

    public bool IsMatchContinue(char c)
    {
        if (IncludeAsciiUnderscore && c == '_')
            return true;
        if (IncludeAtSign && c == '@')
            return true;
        if (IncludeUnicodeLetterOrDigit && char.IsLetterOrDigit(c))
            return true;
        if (IncludeUnicodeLetter && char.IsLetter(c))
            return true;
        return false;
    }
}

public sealed record IdentifierSpecDto(
    IdentifierCharSetDto Start,
    IdentifierCharSetDto Continue,
    bool KeywordsAreReserved
);

public sealed record IdentifierCharSetDto(
    string[] AllowedCategories
);

// =========================
// Numbers
// =========================

public enum DigitSet
{
    Ascii = 0,
}

public sealed record NumberSpec(
    DigitSet Digits,
    bool AllowLeadingDotReal,
    bool RequireDigitAfterDotIfHasLeadingDigits,
    bool AllowTrailingDotReal,
    bool AllowExponent,
    bool AllowUnderscoreSeparators,
    TokenKind IntegerTokenKind,
    TokenKind RealTokenKind
)
{
    public NumberSpecDto ToDto() =>
        new(
            Digits: Digits.ToString(),
            AllowLeadingDotReal: AllowLeadingDotReal,
            RequireDigitAfterDotIfHasLeadingDigits: RequireDigitAfterDotIfHasLeadingDigits,
            AllowTrailingDotReal: AllowTrailingDotReal,
            AllowExponent: AllowExponent,
            AllowUnderscoreSeparators: AllowUnderscoreSeparators,
            IntegerTokenId: (int)IntegerTokenKind,
            IntegerTokenName: IntegerTokenKind.ToString(),
            RealTokenId: (int)RealTokenKind,
            RealTokenName: RealTokenKind.ToString()
        );
}

public sealed record NumberSpecDto(
    string Digits,
    bool AllowLeadingDotReal,
    bool RequireDigitAfterDotIfHasLeadingDigits,
    bool AllowTrailingDotReal,
    bool AllowExponent,
    bool AllowUnderscoreSeparators,
    int IntegerTokenId,
    string IntegerTokenName,
    int RealTokenId,
    string RealTokenName
);

// =========================
// Strings
// =========================

public enum StringEscapeMode
{
    None = 0,
}

public sealed record StringSpec(
    string[] QuoteChars,
    bool TripleQuoteEnabled,
    bool MultiLineRequiresTripleQuote,
    StringEscapeMode EscapeMode,
    bool AllowsNewlineInSingleLineString,
    TokenKind TokenKind
)
{
    public StringSpecDto ToDto() =>
        new(
            QuoteChars: QuoteChars.OrderBy(s => s,
                    StringComparer.Ordinal)
                .ToArray(),
            TripleQuoteEnabled: TripleQuoteEnabled,
            MultiLineRequiresTripleQuote: MultiLineRequiresTripleQuote,
            EscapeMode: EscapeMode.ToString(),
            AllowsNewlineInSingleLineString: AllowsNewlineInSingleLineString,
            TokenId: (int)TokenKind,
            TokenName: TokenKind.ToString()
        );
}

public sealed record StringSpecDto(
    string[] QuoteChars,
    bool TripleQuoteEnabled,
    bool MultiLineRequiresTripleQuote,
    string EscapeMode,
    bool AllowsNewlineInSingleLineString,
    int TokenId,
    string TokenName
);

// =========================
// Trivia
// =========================

public sealed record TriviaSpec(
    string[] WhitespaceChars,
    char Newline,
    char LineCommentStart,
    bool LineCommentEndsAtNewline
)
{
    public TriviaSpecDto ToDto() =>
        new(
            WhitespaceChars: WhitespaceChars.OrderBy(s => s,
                    StringComparer.Ordinal)
                .ToArray(),
            Newline: Newline,
            LineCommentStart: LineCommentStart,
            LineCommentEndsAtNewline: LineCommentEndsAtNewline
        );
}

public sealed record TriviaSpecDto(
    string[] WhitespaceChars,
    char Newline,
    char LineCommentStart,
    bool LineCommentEndsAtNewline
);

// =========================
// ASI / Layout
// =========================

public sealed record ParserLayoutExceptionSpec(
    bool AllowMissingTerminatorAfterInitializerIfNextTokenOnNewLine,
    TokenKind OptionalTerminatorTokenKind
)
{
    public ParserLayoutExceptionDto ToDto() =>
        new(
            AllowMissingTerminatorAfterInitializerIfNextTokenOnNewLine:
            AllowMissingTerminatorAfterInitializerIfNextTokenOnNewLine,
            OptionalTerminatorTokenId: (int)OptionalTerminatorTokenKind,
            OptionalTerminatorTokenName: OptionalTerminatorTokenKind.ToString()
        );
}

public sealed record ParserLayoutExceptionDto(
    bool AllowMissingTerminatorAfterInitializerIfNextTokenOnNewLine,
    int OptionalTerminatorTokenId,
    string OptionalTerminatorTokenName
);

public sealed record AsiSpec(
    bool Enabled,
    TokenKind VirtualTerminatorKind,
    TokenKind NewlineTokenKind,
    bool DropsNewlineTokens,
    TokenKind[] NoInsertAfter,
    TokenKind[] ContinuationBefore,
    ParserLayoutExceptionSpec ParserExceptions
)
{
    [JsonIgnore]
    public HashSet<TokenKind> NoInsertAfterSet { get; } =
    [
        ..NoInsertAfter
    ];

    [JsonIgnore]
    public HashSet<TokenKind> ContinuationBeforeSet { get; } =
    [
        ..ContinuationBefore
    ];

    public AsiSpecDto ToDto() =>
        new(
            Enabled: Enabled,
            VirtualTerminatorTokenId: (int)VirtualTerminatorKind,
            VirtualTerminatorTokenName: VirtualTerminatorKind.ToString(),
            NewlineTokenId: (int)NewlineTokenKind,
            NewlineTokenName: NewlineTokenKind.ToString(),
            DropsNewlineTokens: DropsNewlineTokens,
            NoInsertAfterTokenIds: NoInsertAfter.Select(k => (int)k)
                .OrderBy(x => x)
                .ToArray(),
            NoInsertAfterTokenNames: NoInsertAfter.OrderBy(k => (int)k)
                .Select(k => k.ToString())
                .ToArray(),
            ContinuationBeforeTokenIds: ContinuationBefore.Select(k => (int)k)
                .OrderBy(x => x)
                .ToArray(),
            ContinuationBeforeTokenNames: ContinuationBefore.OrderBy(k => (int)k)
                .Select(k => k.ToString())
                .ToArray(),
            ParserExceptions: ParserExceptions.ToDto()
        );
}

public sealed record AsiSpecDto(
    bool Enabled,
    int VirtualTerminatorTokenId,
    string VirtualTerminatorTokenName,
    int NewlineTokenId,
    string NewlineTokenName,
    bool DropsNewlineTokens,
    int[] NoInsertAfterTokenIds,
    string[] NoInsertAfterTokenNames,
    int[] ContinuationBeforeTokenIds,
    string[] ContinuationBeforeTokenNames,
    ParserLayoutExceptionDto ParserExceptions
);