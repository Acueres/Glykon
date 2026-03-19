using System.Text.Json;
using System.Text.Json.Serialization;

namespace Glykon.Compiler.Syntax;

/// <summary>
/// Glykon lexical rules.
///
/// This spec is designed to be machine-readable (DTO + deterministic JSON)
/// </summary>
public static class LanguageSpec
{
    public const string LanguageName = "Glykon";

    // -------------------------
    // Versioning / hashing
    // -------------------------

    /// <summary>
    /// Manual spec version. Increment when any exported rule/table changes.
    /// </summary>
    public const string SpecVersion = "0.1.0";

    // -------------------------
    // Canonical JSON options
    // -------------------------

    /// <summary>
    /// Canonical options used for hashing and deterministic JSON output
    /// </summary>
    private static readonly JsonSerializerOptions CanonicalJsonOptions = new()
    {
        WriteIndented = false,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
    
    /// <summary>
    /// Grammar
    /// </summary>

    private const string grammarEbnf = """

                                      program         ::= { item } EOF ;
                                      item            ::= declaration | statement ;

                                      (* ---------- Top-level / block-level declarations ---------- *)

                                      declaration     ::= const_decl
                                                        | var_decl
                                                        | func_decl
                                                        | type_decl ;

                                      type_decl       ::= ("class" | "struct") IDENT "{" { type_member } "}" ;

                                      type_member     ::= method_decl
                                                        | const_decl
                                                        | type_decl          (* nested class/struct *)
                                                        | field_decl ;

                                      field_decl      ::= IDENT ":" type [ "=" logical_or ] stmt_end ;

                                      func_decl       ::= "def" IDENT "(" [ param_list ] ")" [ "->" type ] block ;

                                      method_decl     ::= "def" IDENT "(" [ method_param_list ] ")" [ "->" type ] block ;
                                      (* NOTE: first method parameter may omit ": type" (self-style). Other params require ": type". *)

                                      param_list          ::= param { "," param } ;
                                      param               ::= IDENT ":" type ;

                                      method_param_list   ::= method_param { "," method_param } ;
                                      method_param        ::= IDENT [ ":" type ] ;

                                      var_decl         ::= "let" [ "const" ] IDENT [ ":" type ] "=" logical_or stmt_end ;
                                      const_decl       ::= "const" IDENT ":" type "=" logical_or stmt_end ;

                                      type            ::= type_ref ;
                                      type_ref        ::= IDENT { "." IDENT } ;

                                      (* ---------- Statements ---------- *)

                                      statement       ::= block
                                                        | if_stmt
                                                        | while_stmt
                                                        | for_stmt
                                                        | return_stmt
                                                        | jump_stmt
                                                        | expr_stmt ;

                                      block           ::= "{" { item } "}" ;
                                      (* NOTE: item includes statement, and statement includes block. This is intentional nesting. *)

                                      if_stmt         ::= "if" logical_or block
                                                          { "elif" logical_or block }
                                                          [ "else" statement ] ;

                                      while_stmt      ::= "while" logical_or block ;

                                      for_stmt        ::= "for" IDENT "in" range block ;
                                      range           ::= term ( ".." | "..=" ) term [ "by" term ] ;
                                      (* NOTE: range bounds/step parse as term-level expressions (not full logical/comparison). *)

                                      return_stmt     ::= "return" [ logical_or ] stmt_end ;
                                      jump_stmt       ::= ("break" | "continue") stmt_end ;
                                      expr_stmt       ::= expression stmt_end ;

                                      stmt_end        ::= ";" | VIRTUAL_TERMINATOR | ε ;
                                      (* VIRTUAL_TERMINATOR is not written in source; it represents an implicit line terminator / ASI.
                                         Parser accepts implicit termination before "}" / EOF, and also allows ASI after an initializer on a new line. *)

                                      (* ---------- Expressions (precedence, low -> high) ---------- *)

                                      expression      ::= assignment ;

                                      assignment      ::= logical_or [ "=" assignment ] ;
                                      (* LHS must be IDENT or member_access chain; otherwise it's a parse error. *)

                                      logical_or      ::= logical_and { "or"  logical_and } ;
                                      logical_and     ::= equality    { "and" equality    } ;
                                      equality        ::= comparison  { ("==" | "!=") comparison } ;
                                      comparison      ::= term        { (">" | ">=" | "<" | "<=") term } ;
                                      term            ::= factor      { ("+" | "-") factor } ;
                                      factor          ::= unary       { ("*" | "/") unary } ;

                                      unary           ::= ("!" | "-") unary
                                                        | postfix ;

                                      postfix         ::= primary { postfix_op } ;
                                      postfix_op      ::= call_suffix | member_suffix | as_suffix ;

                                      member_suffix   ::= "." IDENT ;
                                      as_suffix       ::= "as" type ;

                                      call_suffix     ::= "(" [ arg_list ] ")" ;
                                      arg_list        ::= logical_or { "," logical_or } ;
                                      (* NOTE: calls are only completed when the callee is an identifier, a member-access, or a parenthesized expression.
                                               After `as`, you generally need parentheses to call: (expr as T)(...) *)

                                      primary         ::= literal
                                                        | IDENT
                                                        | "(" logical_or ")"
                                                        | initializer ;

                                      initializer     ::= "new" type_ref "{"
                                                            [ init_entry { "," init_entry } [ "," ] ]
                                                          "}" ;
                                      init_entry      ::= IDENT ":" logical_or ;

                                      literal         ::= "none" | "true" | "false"
                                                        | INT | REAL | STRING | MULTILINE_STRING ;


                                      """;
    
    private const string grammarPrompt = """
                                              Declarations:
                                                Function: def name(params) [-> Type] { ... }
                                                Type: class Name { ... } or struct Name { ... }
                                                Field inside a type: name: Type [= expr]
                                                Let: let [const] name [: Type] = expr
                                                Const: const name: Type = expr
                                                Parameters: normally name: Type. In methods, the first parameter may omit : Type (self-style).
                                              
                                              Statements
                                                Block: { ... }
                                                If: if expr { ... } {elif expr { ... }} [else stmt]
                                                While: while expr { ... }
                                                For range: for i in a..b { ... } or a..=b and optional step: by step
                                                Return: return [expr]
                                                Break / Continue: break, continue
                                                Expression statement: expr
                                              
                                              Statement termination
                                                ; is allowed, but often optional: a statement can end at a newline or before } or end-of-file.
                                                (Do not output any “ASI/virtual terminator” tokens; that’s internal.)
                                              
                                              Expressions
                                                Usual precedence: assignment =, then or, and, comparisons (== != < <= > >=), + -, * /, unary ! -.
                                              
                                              Postfix operations:
                                                Call: f(args)
                                                Member access: x.y
                                                Cast: expr as Type
                                                Object initializer: new Type { field: expr, ... }
                                                
                                              Builtins and semantics:
                                                Built-in types: int, real, str, bool.
                                                Functions without return do not specify their return type.
                                                `println(x)` prints one line of output. Pass a value of type str; cast with as str when needed.
                                                Use `main()` as the entry point for runnable programs.
                                                When a task asks to print a value, print only the requested value(s).
                                                Do not add explanations, placeholder code, or extra declarations unless needed for the task.
                                              """;

    // -------------------------
    // Keywords
    // -------------------------

    private static readonly KeywordSpec[] keywordEntries =
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

    private static readonly Dictionary<string, TokenKind> keywords;
    
    private static readonly HashSet<TokenKind> rootKeywords = [
        TokenKind.Const,
        TokenKind.Def,
        TokenKind.Class,
        TokenKind.Struct
    ];

    private static readonly SymbolSpec[] symbolEntries =
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
        new(TokenKind.VirtualTerminator,
            "\n"),

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

    // =========================
    // Identifier
    // =========================

    public static class IdentifierSpec
    {
        // Start Pattern
        private const string start = @"re:^(?:\p{L}|_|@)$";

        // Continue Pattern
        private const string cont = @"re:^(?:\p{L}|[0-9]|_)$";

        public static bool IsMatchStart(char c)
        {
            return c == '_' || c == '@' || char.IsLetter(c);
        }

        public static bool IsMatchContinue(char c)
        {
            return c == '_' || char.IsLetter(c) || char.IsAsciiDigit(c);
        }

        public static LexerMachineDto LexerMachine { get; } = GetIdentifierLexerMachineDto();

        private static LexerMachineDto GetIdentifierLexerMachineDto()
        {
            // States:
            // 0 = Start (not accepting)
            // 1 = InIdentifier (accepting)
            return new LexerMachineDto(
                TokenKindId: (int)TokenKind.Identifier,
                StartStateId: 0,
                States:
                [
                    new LexerStateDto(Id: 0, Accepting: false),
                    new LexerStateDto(Id: 1, Accepting: true),
                ],
                Transitions:
                [
                    // Start -> InIdentifier on valid start char
                    new LexerTransitionDto(FromStateId: 0, Predicate: start, ToStateId: 1),

                    // Stay in identifier on valid continue char
                    new LexerTransitionDto(FromStateId: 1, Predicate: cont, ToStateId: 1),
                ],
                32
            );
        }
    }

    // =========================
    // Strings
    // =========================

    public static class StringSpec
    {
        private static HashSet<char> stringQuoteChars { get; } = ['\'', '"'];

        public static bool IsStringQuote(char c) => stringQuoteChars.Contains(c);

        public static LexerMachineDto SingleLineLexerMachine { get; } = GetSingleLineLexerMachineDto();
        public static LexerMachineDto MultilineLexerMachine { get; } = GetMultilineLexerMachineDto();
        
        private static LexerMachineDto GetSingleLineLexerMachineDto()
        {
            // States:
            // 0 Start
            // Single-quote branch:
            // 1 InSingle, 2 EscSingle, 3 DoneSingle (accepting)
            // Double-quote branch:
            // 4 InDouble, 5 EscDouble, 6 DoneDouble (accepting)

            var states = new[]
            {
                new LexerStateDto(0, false),
                new LexerStateDto(1, false),
                new LexerStateDto(2, false),
                new LexerStateDto(3, true),
                new LexerStateDto(4, false),
                new LexerStateDto(5, false),
                new LexerStateDto(6, true),
            };

            // Helper regexes
            // In single-quoted string: anything but newline, backslash, single-quote
            const string ReInSingle = @"re:^(?!\n|'|\\).$";
            // In double-quoted string: anything but newline, backslash, double-quote
            const string ReInDouble = @"re:^(?!\n|""|\\).$";
            // Escape payload: any char but newline
            const string ReEscPayload = @"re:^(?!\n).$";

            var tr = new List<LexerTransitionDto>
            {
                // Start transitions
                new(0, "lit:'", 1),
                new(0, "lit:\"", 4),

                // Single-quote branch
                new(1, "lit:\\", 2), // backslash escape
                new(1, "lit:'", 3), // closing '
                new(1, ReInSingle, 1), // normal char

                new(2, ReEscPayload, 1), // escaped char

                // Double-quote branch
                new(4, "lit:\\", 5),
                new(4, "lit:\"", 6),
                new(4, ReInDouble, 4),

                new(5, ReEscPayload, 4),
            };

            return new LexerMachineDto(TokenKindId: (int)TokenKind.LiteralString, StartStateId: 0, States: states,
                Transitions: tr.ToArray(), 200_000);
        }

        private static LexerMachineDto GetMultilineLexerMachineDto()
        {
            // This machine recognizes:
            //   ''' ... '''
            //   """ ... """
            //
            // Branches are duplicated. It allows any char inside (including '\n').
            // It detects closing triple quotes via intermediate states.

            // Single-quote branch:
            // 0 Start
            // 1 saw '
            // 2 saw ''
            // 3 InTripleSingle content
            // 4 saw closing ' (1st)
            // 5 saw closing '' (2nd)
            // 6 DoneSingle (accepting)

            // Double-quote branch:
            // 7 saw "
            // 8 saw ""
            // 9 InTripleDouble content
            // 10 saw closing " (1st)
            // 11 saw closing "" (2nd)
            // 12 DoneDouble (accepting)

            var states = new[]
            {
                new LexerStateDto(0, false),
                new LexerStateDto(1, false),
                new LexerStateDto(2, false),
                new LexerStateDto(3, false),
                new LexerStateDto(4, false),
                new LexerStateDto(5, false),
                new LexerStateDto(6, true),

                new LexerStateDto(7, false),
                new LexerStateDto(8, false),
                new LexerStateDto(9, false),
                new LexerStateDto(10, false),
                new LexerStateDto(11, false),
                new LexerStateDto(12, true),
            };

            var tr = new List<LexerTransitionDto>
            {
                // Start triple single: '''
                new(0, "lit:'", 1),
                new(1, "lit:'", 2),
                new(2, "lit:'", 3),

                // InTripleSingle content
                new(3, "lit:'", 4), // maybe starting close
                new(3, "*", 3), // any other char (including newline)

                // Closing detector for single quotes
                new(4, "lit:'", 5), // got ''
                new(4, "*", 3), // false alarm: go back to content

                new(5, "lit:'", 6), // got ''' -> done
                new(5, "*", 3), // false alarm

                // Start triple double: """
                new(0, "lit:\"", 7),
                new(7, "lit:\"", 8),
                new(8, "lit:\"", 9),

                // InTripleDouble content
                new(9, "lit:\"", 10),
                new(9, "*", 9),

                // Closing detector for double quotes
                new(10, "lit:\"", 11),
                new(10, "*", 9),

                new(11, "lit:\"", 12),
                new(11, "*", 9),
            };

            return new LexerMachineDto(TokenKindId: (int)TokenKind.LiteralMultilineString, StartStateId: 0,
                States: states,
                Transitions: tr.ToArray(), 200_000);
        }
    }

    // =========================
    // Numbers
    // =========================

    public static class NumberSpec
    {
        public static TokenKind IntegerTokenKind => TokenKind.LiteralInt;
        public static TokenKind RealTokenKind => TokenKind.LiteralReal;
        
        private const string Digit = @"re:^[0-9]$";
        private const string Dot = "lit:.";
        
        public static LexerMachineDto IntegerLexerMachine { get; } = GetIntegerLexerMachineDto();
        public static LexerMachineDto RealLexerMachine { get; } = GetRealLexerMachineDto();

        private static LexerMachineDto GetIntegerLexerMachineDto()
        {
            // States:
            // 0 = Start
            // 1 = Digits (accepting)
            return new LexerMachineDto(
                TokenKindId: (int)TokenKind.LiteralInt,
                StartStateId: 0,
                States:
                [
                    new LexerStateDto(0, Accepting: false),
                    new LexerStateDto(1, Accepting: true),
                ],
                Transitions:
                [
                    new LexerTransitionDto(0, Predicate: Digit, ToStateId: 1),
                    new LexerTransitionDto(1, Predicate: Digit, ToStateId: 1),
                ],
                MaxLexemeChars: 128
            );
        }

        private static LexerMachineDto GetRealLexerMachineDto()
        {
            // Reals supported (no sci-notation):
            //   - \d+\.\d+
            //   - \.\d+
            //
            // Important property for generation:
            //   We allow consuming a leading integer prefix (\d+) but it's NOT accepting
            //   until we have '.' and at least one fractional digit.

            // States:
            // 0 = Start (not accepting)
            // 1 = IntPart (\d+) (NOT accepting)
            // 2 = AfterDotFromInt (\d+\.) (NOT accepting)
            // 3 = FracPart (\d+\.\d+ OR \.\d+) (accepting)
            // 4 = LeadingDotSeen (.) (NOT accepting)
            return new LexerMachineDto(
                TokenKindId: (int)TokenKind.LiteralReal,
                StartStateId: 0,
                States:
                [
                    new LexerStateDto(0, Accepting: false),
                    new LexerStateDto(1, Accepting: false),
                    new LexerStateDto(2, Accepting: false),
                    new LexerStateDto(3, Accepting: true),
                    new LexerStateDto(4, Accepting: false),
                ],
                Transitions:
                [
                    // Start
                    new LexerTransitionDto(0, Predicate: Digit, ToStateId: 1),
                    new LexerTransitionDto(0, Predicate: Dot, ToStateId: 4),

                    // Int part
                    new LexerTransitionDto(1, Predicate: Digit, ToStateId: 1),
                    new LexerTransitionDto(1, Predicate: Dot, ToStateId: 2),

                    // After dot (from int)
                    new LexerTransitionDto(2, Predicate: Digit, ToStateId: 3),

                    // Leading dot seen
                    new LexerTransitionDto(4, Predicate: Digit, ToStateId: 3),

                    // Fractional digits
                    new LexerTransitionDto(3, Predicate: Digit, ToStateId: 3),
                ],
                MaxLexemeChars: 128
            );
        }
    }

    // =========================
    // Trivia
    // =========================

    public static class TriviaSpec
    {
        public static char NewLineChar => '\n';
        public static TokenKind NewlineTokenKind => TokenKind.Newline;
        
        private static HashSet<char> whitespaceChars { get; } = [' ', '\t', '\r'];
        
        public static char LineCommentStart => '#';
        
        public static TriviaDto Dto { get; } = new(whitespaceChars.Select(c => c.ToString()).ToArray(), NewLineChar.ToString(),
            LineCommentStart.ToString());
        
        public static bool IsWhitespace(char c) => whitespaceChars.Contains(c);
    }

    // =========================
    // ASI
    // =========================

    public static class AsiSpec
    {
        private static readonly HashSet<TokenKind> noInsertAfter =
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

        private static readonly HashSet<TokenKind> continuationBefore =
        [
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

        public static TokenKind VirtualTerminatorKind => TokenKind.VirtualTerminator;

        public static bool NoInsertAfter(TokenKind previous) =>
            noInsertAfter.Contains(previous);

        public static bool ContinuationBefore(TokenKind next) =>
            continuationBefore.Contains(next);
    }

    // -------------------------
    // Synthetic tokens
    // -------------------------

    private static readonly HashSet<TokenKind> syntheticTokenKinds =
    [
        TokenKind.EOF,
        TokenKind.Empty,
        TokenKind.Cursor
    ];
    
    // -------------------------
    // Pattern tokens
    // -------------------------
    
    private static readonly HashSet<TokenKind> patternTokenKinds =
    [
        TokenKind.LiteralInt,
        TokenKind.LiteralReal,
        TokenKind.LiteralString,
        TokenKind.LiteralMultilineString,
        TokenKind.Identifier
    ];

    // -------------------------
    // Initialization
    // -------------------------

    static LanguageSpec()
    {
        // Build keyword dictionary
        keywords = keywordEntries
            .OrderBy(k => k.Text,
                StringComparer.Ordinal)
            .ToDictionary(k => k.Text,
                k => k.Kind,
                StringComparer.Ordinal);
    }

    // -------------------------
    // Public export API
    // -------------------------

    public static LanguageSpecDto ToDto()
    {
        List<TokenInfoDto> tokenInfos = [];
        List<FixedTokenDto> fixedTokens = [];
        List<FixedTokenDto> rootTokens = [];

        Dictionary<TokenKind, string> fixedMap = [];

        foreach (var kw in keywordEntries)
        {
            fixedMap[kw.Kind] = kw.Text;
        }

        foreach (var sb in symbolEntries)
        {
            fixedMap[sb.Kind] = sb.Spelling;
        }

        foreach (var kind in Enum.GetValues<TokenKind>())
        {
            tokenInfos.Add(new TokenInfoDto((int)kind, kind.ToString()));

            if (syntheticTokenKinds.Contains(kind) || patternTokenKinds.Contains(kind)) continue;

            string literal = fixedMap[kind];

            var fixedToken = new FixedTokenDto((int)kind, kind.ToString(), literal);
            fixedTokens.Add(fixedToken);

            if (rootKeywords.Contains(kind))
            {
                rootTokens.Add(fixedToken);
            }
        }

        LexerMachineDto[] lexerMachines =
        [
            StringSpec.SingleLineLexerMachine, StringSpec.MultilineLexerMachine, IdentifierSpec.LexerMachine,
            NumberSpec.IntegerLexerMachine, NumberSpec.RealLexerMachine
        ];

        return new LanguageSpecDto(SpecVersion, grammarEbnf, grammarPrompt,tokenInfos.ToArray(),
            fixedTokens.ToArray(), rootTokens.ToArray(),
            lexerMachines, TriviaSpec.Dto, syntheticTokenKinds.Select(k => (int)k).ToArray());
    }

    public static string ToJson(JsonSerializerOptions? opts = null)
    {
        opts ??= CanonicalJsonOptions;
        return JsonSerializer.Serialize(ToDto(),
            opts);
    }

    public static byte[] ToJsonUtf8(JsonSerializerOptions? opts = null)
    {
        opts ??= CanonicalJsonOptions;
        return JsonSerializer.SerializeToUtf8Bytes(ToDto(),
            opts);
    }

    // -------------------------
    // Shared helpers
    // -------------------------

    public static bool TryGetKeywordKind(string text,
        out TokenKind kind) =>
        keywords.TryGetValue(text,
            out kind);

    public static IEnumerable<SymbolSpec> GetSymbolSpecs()
    {
        foreach (var symbol in symbolEntries)
        {
            yield return symbol;
        }
    }

    // -------------------------
    // Spec primitives
    // -------------------------

    private readonly record struct KeywordSpec(
        string Text,
        TokenKind Kind);

    public readonly record struct SymbolSpec(
        TokenKind Kind,
        string Spelling);
}

// =========================
// DTOs for JSON export
// =========================

public sealed record LanguageSpecDto(
    string SpecVersion,
    string GrammarEbnf,
    string GrammarPrompt,
    TokenInfoDto[] Tokens,
    FixedTokenDto[] FixedTokens,
    FixedTokenDto[] RootTokens,
    LexerMachineDto[] LexerMachines,
    TriviaDto Trivia,
    int[] IgnoredTokenKindIds
);

public sealed record TokenInfoDto(int Id, string Name);

public sealed record FixedTokenDto(int Id, string Name, string Literal);

public sealed record TriviaDto(
    string[] WhitespaceChars,
    string NewlineChar,
    string LineCommentStart
);

public sealed record LexerMachineDto(
    int TokenKindId,
    int StartStateId,
    LexerStateDto[] States,
    LexerTransitionDto[] Transitions,
    int MaxLexemeChars
);

public sealed record LexerStateDto(
    int Id,
    bool Accepting
);

public sealed record LexerTransitionDto(
    int FromStateId,
    string Predicate,
    int ToStateId
);