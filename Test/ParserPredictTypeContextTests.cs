using Tests.Infrastructure;
using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax;

namespace Tests;

public class ParserPredictTypeContextTests : CompilerTestBase
{
    private void AssertTypeContext(string src, bool expectedTypeContext)
    {
        var (_, _, lexErrors, parseErrors,
            _, isTypeNameContext, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        Assert.Equal(expectedTypeContext, isTypeNameContext);
    }

    private void AssertExpectedContainsWithTypeContext(string src, bool expectedTypeContext,
        params TokenKind[] mustContain)
    {
        var (_, _, lexErrors, parseErrors,
            expected, isTypeNameContext, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        Assert.Equal(expectedTypeContext, isTypeNameContext);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        foreach (var k in mustContain)
            Assert.Contains(k, actual);
    }

    private void AssertExpectedExactlyWithTypeContext(string src, bool expectedTypeContext,
        params TokenKind[] expectedKinds)
    {
        var (_, _, lexErrors, parseErrors,
            expected, isTypeNameContext, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        Assert.Equal(expectedTypeContext, isTypeNameContext);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        Assert.Equal(expectedKinds.ToHashSet(), actual);
    }

    // --------------------------
    // POSITIVE: TypeName contexts
    // --------------------------

    [Fact]
    public void Predict_const_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("const x:", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_const_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("const x: Outer.", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_let_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("let x:", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_let_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("let x: Outer.", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_param_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("def f(x:", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_param_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("def f(x: Outer.", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_return_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("def f() ->", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_return_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("def f() -> Outer.", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_as_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("x as", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_as_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("x as Outer.", expectedTypeContext: true, TokenKind.Identifier);
    
    [Fact]
    public void Predict_field_type_slot_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("class A { x:", expectedTypeContext: true, TokenKind.Identifier);

    [Fact]
    public void Predict_field_type_qualified_part_sets_type_context()
        => AssertExpectedExactlyWithTypeContext("class A { x: Outer.", expectedTypeContext: true, TokenKind.Identifier);

    // ---------------------------------
    // NEGATIVE: Identifier is NOT a type
    // ---------------------------------

    [Fact]
    public void Predict_const_name_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("const", expectedTypeContext: false, TokenKind.Identifier);

    [Fact]
    public void Predict_let_name_is_not_type_context()
        // "let" expects either "const" or identifier (variable name), not a type.
        => AssertExpectedExactlyWithTypeContext("let", expectedTypeContext: false, TokenKind.Const,
            TokenKind.Identifier);

    [Fact]
    public void Predict_def_name_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("def", expectedTypeContext: false, TokenKind.Identifier);

    [Fact]
    public void Predict_param_name_is_not_type_context()
        // At "def f(" we expect parameter name identifier, not a type.
        => AssertExpectedExactlyWithTypeContext("def f(", expectedTypeContext: false, TokenKind.ParenthesisRight,
            TokenKind.Identifier);

    [Fact]
    public void Predict_class_name_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("class", expectedTypeContext: false, TokenKind.Identifier);

    [Fact]
    public void Predict_struct_name_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("struct", expectedTypeContext: false, TokenKind.Identifier);

    [Fact]
    public void Predict_member_access_identifier_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("x.", expectedTypeContext: false, TokenKind.Identifier);

    [Fact]
    public void Predict_call_member_chain_identifier_is_not_type_context()
        => AssertExpectedExactlyWithTypeContext("make(1).", expectedTypeContext: false, TokenKind.Identifier);

    // -----------------------------------------
    // Sanity: whenever type context is true,
    // Identifier MUST be among expected token kinds
    // -----------------------------------------

    [Fact]
    public void Predict_type_context_always_includes_identifier()
        => AssertExpectedContainsWithTypeContext("def f(x:", expectedTypeContext: true, TokenKind.Identifier);
}