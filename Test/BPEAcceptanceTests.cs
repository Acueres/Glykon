using Glykon.Compiler.Syntax;
using Tests.Infrastructure;

namespace Tests;

public class BPEAcceptanceTests
{
    private static readonly Dictionary<TokenKind, string> fixedSpellings = new()
    {
        { TokenKind.Def, "def" },
        { TokenKind.Minus, "-" },
        { TokenKind.Arrow, "->" },
        { TokenKind.Dot, "." },

        { TokenKind.BraceLeft, "{" },
        { TokenKind.BraceRight, "}" },
        { TokenKind.ParenthesisLeft, "(" },
        { TokenKind.ParenthesisRight, ")" },
        { TokenKind.Comma, "," },
        { TokenKind.Colon, ":" },
    };
    
    // Pattern machines used by the acceptor
    
    private static readonly LexerMachineDto identifierMachine =
        LanguageSpec.IdentifierSpec.LexerMachine;
    private static readonly LexerMachineDto intMachine = LanguageSpec.NumberSpec.IntegerLexerMachine;
    private static readonly LexerMachineDto realMachine = LanguageSpec.NumberSpec.RealLexerMachine;
    
    private static readonly Dictionary<TokenKind, LexerMachineDto> patternMachines = new()
    {
        { TokenKind.Identifier, identifierMachine },
        { TokenKind.LiteralInt, intMachine },
        { TokenKind.LiteralReal, realMachine }
    };

    [Fact]
    public void BPE_accepts_trivia_prefix_and_suffix_around_fixed_punct()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // " {\n" should be accepted when "{" is expected.
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.BraceLeft],
            piece: " {\n",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary); // brace + newline should finish token
    }

    [Fact]
    public void BPE_handles_multi_piece_fixed_token_arrow()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Arrow],
            piece: "-",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary); // still building "->"

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Arrow],
            piece: ">",
            fixedSpellings,
            patternMachines,
            out ctx));

        // "->" is punctuator-like => self-delimiting => should auto-close
        Assert.True(ctx.IsBoundary);
    }

    [Fact]
    public void BPE_resolves_minus_vs_arrow_using_followup_piece()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // "-" can be Minus OR prefix of Arrow
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Minus, TokenKind.Arrow],
            piece: "-",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        // If next is trivia, we should be able to close as Minus (Arrow path dies).
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Minus, TokenKind.Arrow],
            piece: " ",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary);
    }

    [Fact]
    public void BPE_rejects_trivia_closure_when_real_not_accepting()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // "12." should be a valid REAL prefix (non-accepting), but must not be closable by whitespace.
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: "12.",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        Assert.False(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: " ",
            fixedSpellings,
            patternMachines,
            out _));
    }

    [Fact]
    public void BPE_accepts_real_built_across_pieces_then_allows_trailing_trivia()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // real prefix: digits
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: "12",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        // dot
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: ".",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        // frac digits
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: "34",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        // now it is accepting => whitespace can close
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: " ",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary);
    }

    [Fact]
    public void BPE_keyword_requires_delimiter_before_identifier_continuation()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // Step 1: produce "def" while expecting the keyword def
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Def],
            piece: "def",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary); // keyword is NOT self-delimiting; needs delimiter to commit

        // Step 2: if next piece starts with an identifier-continue char ('a'),
        // we MUST NOT treat it as a new token boundary; otherwise we'd accept "def" + "add" => "defadd" split,
        // which is lexically wrong. This must be rejected.
        Assert.False(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Identifier],
            piece: "add",
            fixedSpellings,
            patternMachines,
            out _));
    }

    [Fact]
    public void BPE_keyword_allows_delimiter_then_identifier()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Def],
            piece: "def",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Identifier],
            piece: " ",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary);

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Identifier],
            piece: "add",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary); // identifier can continue
    }

    [Fact]
    public void BPE_allows_identifier_to_punct_without_whitespace_via_delimiter_restart()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Identifier],
            piece: "add",
            fixedSpellings,
            patternMachines,
            out ctx));

        // Now we expect "(" next, with no whitespace. '(' is a delimiter for identifiers.
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.ParenthesisLeft],
            piece: "(",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary); // punct should auto-close
    }

    [Fact]
    public void BPE_handles_dot_as_punct_or_real_prefix()
    {
        var ctx = BpePieceAcceptor.Context.Empty;

        // "." can be Dot OR start of leading-dot real (".5")
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.Dot, TokenKind.LiteralReal],
            piece: ".",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary); // must stay open because REAL path is non-accepting

        // Continue as real:
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: "5",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.False(ctx.IsBoundary);

        // Close with trivia
        Assert.True(BpePieceAcceptor.TryAdvance(
            ctx,
            expected: [TokenKind.LiteralReal],
            piece: " ",
            fixedSpellings,
            patternMachines,
            out ctx));

        Assert.True(ctx.IsBoundary);
    }
}