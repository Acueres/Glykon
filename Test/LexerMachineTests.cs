using Glykon.Compiler.Syntax;
using Tests.Infrastructure;

namespace Tests;

public class LexerMachineTests
{
    // =========================
    // String machines tests
    // =========================

    private static readonly LexerMachineDto stringSingleLineMachine =
        LanguageSpec.StringSpec.SingleLineLexerMachine;

    private static readonly LexerMachineDto stringMultilineMachine =
        LanguageSpec.StringSpec.MultilineLexerMachine;

    [Fact]
    public void SingleLine_accepts_basic_single_quotes()
    {
        var m = stringSingleLineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "'abc'"));
    }

    [Fact]
    public void SingleLine_accepts_basic_double_quotes()
    {
        var m = stringSingleLineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "\"abc\""));
    }

    [Fact]
    public void SingleLine_rejects_newline()
    {
        var m = stringSingleLineMachine;
        Assert.False(LexerMachineRunner.Accepts(m, "'a\nb'"));
    }

    [Fact]
    public void SingleLine_accepts_escape_sequences()
    {
        var m = stringSingleLineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "'a\\'b'")); // escaped quote
        Assert.True(LexerMachineRunner.Accepts(m, "\"a\\\"b\"")); // escaped quote
        Assert.True(LexerMachineRunner.Accepts(m, "'a\\\\b'")); // escaped backslash
    }

    [Fact]
    public void SingleLine_rejects_unterminated()
    {
        var m = stringSingleLineMachine;
        Assert.False(LexerMachineRunner.Accepts(m, "'abc"));
        Assert.False(LexerMachineRunner.Accepts(m, "\"abc"));
    }

    [Fact]
    public void MultiLine_accepts_triple_single_quotes_with_newlines()
    {
        var m = stringMultilineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "'''a\nb'''"));
    }

    [Fact]
    public void MultiLine_accepts_triple_double_quotes_with_newlines()
    {
        var m = stringMultilineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "\"\"\"a\nb\"\"\""));
    }

    [Fact]
    public void MultiLine_rejects_unterminated()
    {
        var m = stringMultilineMachine;
        Assert.False(LexerMachineRunner.Accepts(m, "'''a\nb''"));
        Assert.False(LexerMachineRunner.Accepts(m, "\"\"\"a\nb\"\""));
    }

    [Fact]
    public void MultiLine_handles_quotes_inside_content()
    {
        var m = stringMultilineMachine;
        Assert.True(LexerMachineRunner.Accepts(m, "'''a''x'''")); // two quotes are content, not terminator
        Assert.True(LexerMachineRunner.Accepts(m, "\"\"\"a\"\"x\"\"\""));
    }

    // =========================
    // Identifier machine tests
    // =========================

    private static readonly LexerMachineDto identifierMachine =
        LanguageSpec.IdentifierSpec.LexerMachine;

    [Fact]
    public void Identifier_accepts_simple_ascii()
    {
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "a"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "abc"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "a_b"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "abc123"));
    }

    [Fact]
    public void Identifier_accepts_underscore_and_at_start()
    {
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "_"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "_x"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "@x"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "@1")); // digits are allowed in continuation
    }

    [Fact]
    public void Identifier_accepts_unicode_letters()
    {
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "Привет"));
        Assert.True(LexerMachineRunner.Accepts(identifierMachine, "λx"));
    }

    [Fact]
    public void Identifier_rejects_starting_with_digit()
    {
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, "1"));
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, "1abc"));
    }

    [Fact]
    public void Identifier_rejects_invalid_characters()
    {
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, "a-b"));
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, "a b"));
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, "a."));
        Assert.False(LexerMachineRunner.Accepts(identifierMachine, ""));
    }

    // =========================
    // Integer machine tests
    // =========================
    
    private static readonly LexerMachineDto intMachine = LanguageSpec.NumberSpec.IntegerLexerMachine;

    [Fact]
    public void Integer_accepts_digits()
    {
        Assert.True(LexerMachineRunner.Accepts(intMachine, "0"));
        Assert.True(LexerMachineRunner.Accepts(intMachine, "123"));
        Assert.True(LexerMachineRunner.Accepts(intMachine, "007"));
    }

    [Fact]
    public void Integer_rejects_non_digits()
    {
        Assert.False(LexerMachineRunner.Accepts(intMachine, ""));
        Assert.False(LexerMachineRunner.Accepts(intMachine, "1.0"));
        Assert.False(LexerMachineRunner.Accepts(intMachine, "-1"));
        Assert.False(LexerMachineRunner.Accepts(intMachine, "1e2"));
        Assert.False(LexerMachineRunner.Accepts(intMachine, "12_34"));
    }

    // =========================
    // Real machine tests
    // =========================
    
    private static readonly LexerMachineDto realMachine = LanguageSpec.NumberSpec.RealLexerMachine;

    [Fact]
    public void Real_accepts_decimal_forms()
    {
        Assert.True(LexerMachineRunner.Accepts(realMachine, "0.0"));
        Assert.True(LexerMachineRunner.Accepts(realMachine, "12.34"));
        Assert.True(LexerMachineRunner.Accepts(realMachine, ".5"));
        Assert.True(LexerMachineRunner.Accepts(realMachine, ".0001"));
    }

    [Fact]
    public void Real_rejects_integer_only_and_incomplete_decimal()
    {
        Assert.False(LexerMachineRunner.Accepts(realMachine, "12")); // must have dot+frac
        Assert.False(LexerMachineRunner.Accepts(realMachine, "12.")); // must have at least one frac digit
        Assert.False(LexerMachineRunner.Accepts(realMachine, ".")); // must have at least one digit
    }

    [Fact]
    public void Real_rejects_scientific_notation_and_multiple_dots()
    {
        Assert.False(LexerMachineRunner.Accepts(realMachine, "1e2"));
        Assert.False(LexerMachineRunner.Accepts(realMachine, "1.2e3"));
        Assert.False(LexerMachineRunner.Accepts(realMachine, "1.2.3"));
    }
}