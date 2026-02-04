using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax;
using Tests.Infrastructure;

namespace Tests;

public sealed class ParserPredictExpectedTokensTests : CompilerTestBase
{
    private static HashSet<TokenKind> Set(params TokenKind[] kinds) => kinds.ToHashSet();

    private void AssertExpectedExactly(string src, params TokenKind[] expectedKinds)
    {
        var (_, _, lexErrors, parseErrors, expected) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        Assert.Equal(Set(expectedKinds), actual);
    }

    private void AssertExpectedContains(string src, params TokenKind[] mustContain)
    {
        var (_, _, lexErrors, parseErrors, expected) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        foreach (var k in mustContain)
            Assert.Contains(k, actual);
    }
    
    // CONST DECLARATIONS

    [Fact]
    public void Predict_const_expects_identifier()
        => AssertExpectedExactly("const", TokenKind.Identifier);

    [Fact]
    public void Predict_const_name_expects_colon()
        => AssertExpectedExactly("const x", TokenKind.Colon);

    [Fact]
    public void Predict_const_name_colon_expects_type_identifier()
        => AssertExpectedExactly("const x:", TokenKind.Identifier);

    [Fact]
    public void Predict_const_type_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("const x: Outer.", TokenKind.Identifier);

    [Fact]
    public void Predict_const_type_deep_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("const x: Outer.Inner.", TokenKind.Identifier);

    [Fact]
    public void Predict_const_typed_expects_assignment()
        => AssertExpectedExactly("const x: int", TokenKind.Assignment, TokenKind.Dot);

    [Fact]
    public void Predict_const_typed_qualified_expects_assignment()
        => AssertExpectedExactly("const x: Outer.Inner", TokenKind.Assignment, TokenKind.Dot);
    
    // LET DECLARATIONS

    [Fact]
    public void Predict_let_expects_const_or_identifier()
        => AssertExpectedExactly("let", TokenKind.Const, TokenKind.Identifier);

    [Fact]
    public void Predict_let_const_expects_identifier()
        => AssertExpectedExactly("let const", TokenKind.Identifier);

    [Fact]
    public void Predict_let_name_colon_expects_type_identifier()
        => AssertExpectedExactly("let const x:", TokenKind.Identifier);

    [Fact]
    public void Predict_let_type_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("let const x: Outer.", TokenKind.Identifier);
    
    // FUNCTION DECLARATIONS

    [Fact]
    public void Predict_def_expects_identifier()
        => AssertExpectedExactly("def", TokenKind.Identifier);

    [Fact]
    public void Predict_def_name_expects_lparen()
        => AssertExpectedExactly("def f", TokenKind.ParenthesisLeft);

    [Fact]
    public void Predict_def_lparen_expects_param_or_rparen()
        => AssertExpectedExactly("def f(", TokenKind.ParenthesisRight, TokenKind.Identifier);

    [Fact]
    public void Predict_def_first_param_name_expects_colon()
        // In non-method params, after "<ident>" we must Consume(':')
        => AssertExpectedExactly("def f(x", TokenKind.Colon);

    [Fact]
    public void Predict_def_param_colon_expects_type_identifier()
        => AssertExpectedExactly("def f(x:", TokenKind.Identifier);

    [Fact]
    public void Predict_def_param_type_expects_comma_or_rparen()
        => AssertExpectedExactly("def f(x: int", TokenKind.Comma, TokenKind.ParenthesisRight, TokenKind.Dot);

    [Fact]
    public void Predict_def_param_comma_expects_next_param_identifier()
        => AssertExpectedExactly("def f(x: int,", TokenKind.Identifier);

    [Fact]
    public void Predict_def_rparen_expects_arrow_or_lbrace()
        // After ')': Match(Arrow) records Arrow, then Consume('{') stops.
        => AssertExpectedExactly("def f()", TokenKind.Arrow, TokenKind.BraceLeft);

    [Fact]
    public void Predict_def_arrow_expects_return_type_identifier()
        => AssertExpectedExactly("def f() ->", TokenKind.Identifier);

    [Fact]
    public void Predict_def_return_type_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("def f() -> Outer.", TokenKind.Identifier);

    [Fact]
    public void Predict_def_return_type_expects_lbrace()
        => AssertExpectedExactly("def f() -> int", TokenKind.BraceLeft, TokenKind.Dot);
    
    // FUNCTION BODIES

    [Fact]
    public void Predict_function_body_empty_contains_rbrace()
        // After consuming '{' the block may end immediately.
        => AssertExpectedContains("def main() {", TokenKind.BraceRight);

    [Fact]
    public void Predict_function_body_after_expression_stmt_contains_rbrace()
        // Regression: when a statement can end via a virtual terminator at the cursor,
        // the parser must still report '}' as a valid next token inside a block.
        => AssertExpectedContains("def main() { println('x')", TokenKind.BraceRight);

    [Fact]
    public void Predict_function_body_after_return_value_contains_rbrace()
        // Same regression, but via ReturnStmt + TerminateStatement.
        => AssertExpectedContains("def main() { return 1", TokenKind.BraceRight);

    [Fact]
    public void Predict_function_body_after_let_declaration_contains_rbrace()
        // Same regression, but via variable declaration termination.
        => AssertExpectedContains("def main() { let x = 1", TokenKind.BraceRight);

    [Fact]
    public void Predict_nested_block_after_expression_stmt_contains_rbrace()
        // Regression: nested blocks must still include the inner '}' as a valid next token.
        => AssertExpectedContains("def main() { if true { println('x')", TokenKind.BraceRight);
    
    // TYPE DECLARATIONS

    [Fact]
    public void Predict_class_expects_identifier()
        => AssertExpectedExactly("class", TokenKind.Identifier);

    [Fact]
    public void Predict_class_name_expects_lbrace()
        => AssertExpectedExactly("class A", TokenKind.BraceLeft);

    [Fact]
    public void Predict_struct_expects_identifier()
        => AssertExpectedExactly("struct", TokenKind.Identifier);

    [Fact]
    public void Predict_struct_name_expects_lbrace()
        => AssertExpectedExactly("struct S", TokenKind.BraceLeft);

    [Fact]
    public void Predict_class_body_def_expects_method_name()
        => AssertExpectedExactly("class A { def", TokenKind.Identifier);

    [Fact]
    public void Predict_class_body_const_expects_const_name()
        => AssertExpectedExactly("class A { const", TokenKind.Identifier);

    [Fact]
    public void Predict_class_body_nested_class_expects_identifier()
        => AssertExpectedExactly("class A { class", TokenKind.Identifier);

    [Fact]
    public void Predict_class_body_nested_struct_expects_identifier()
        => AssertExpectedExactly("class A { struct", TokenKind.Identifier);

    [Fact]
    public void Predict_class_body_field_name_expects_colon()
        => AssertExpectedExactly("class A { x", TokenKind.Colon);

    [Fact]
    public void Predict_class_body_field_colon_expects_type_identifier()
        => AssertExpectedExactly("class A { x:", TokenKind.Identifier);

    [Fact]
    public void Predict_class_body_field_type_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("class A { x: Outer.", TokenKind.Identifier);
    
    // METHOD PARAMETER SPECIAL CASE

    [Fact]
    public void Predict_method_first_param_name_expects_colon_or_comma_or_rparen()
        // In methods: first param may omit type if next isn't ':'
        // So after "this" we can have:
        // - ':' (typed param),
        // - ',' (more params),
        // - ')' (end params)
        => AssertExpectedExactly("class A { def m(this", TokenKind.Colon, TokenKind.Comma, TokenKind.ParenthesisRight);

    [Fact]
    public void Predict_method_second_param_name_expects_colon()
        // After "this, x" we must Consume(':') for typed params (not first param anymore)
        => AssertExpectedExactly("class A { def m(this, x", TokenKind.Colon);

    [Fact]
    public void Predict_method_rparen_expects_arrow_or_lbrace()
        => AssertExpectedExactly("class A { def m(this)", TokenKind.Arrow, TokenKind.BraceLeft);
    
    // CONTROL FLOW STATEMENTS

    [Fact]
    public void Predict_if_condition_contains_lbrace()
        // ParseIfStatement: Match(Semicolon) then Consume('{')
        => AssertExpectedContains("if true", TokenKind.BraceLeft);

    /*[Fact]
    public void Predict_if_condition_may_also_expect_semicolon()
        => AssertExpectedContains("if true", TokenKind.Semicolon);*/

    /*[Fact]
    public void Predict_if_condition_after_explicit_semicolon_expects_lbrace_only()
        => AssertExpectedExactly("if true;", TokenKind.BraceLeft);*/

    [Fact]
    public void Predict_while_condition_contains_lbrace()
        => AssertExpectedContains("while true", TokenKind.BraceLeft);

    /*[Fact]
    public void Predict_while_condition_after_explicit_semicolon_expects_lbrace_only()
        => AssertExpectedExactly("while true;", TokenKind.BraceLeft);*/
    
    [Fact]
    public void Predict_if_body_after_expression_stmt_contains_rbrace()
        // Regression: closing brace must be visible as a valid continuation inside if bodies.
        => AssertExpectedContains("if true { println('x')", TokenKind.BraceRight);

    [Fact]
    public void Predict_while_body_after_expression_stmt_contains_rbrace()
        // Regression: closing brace must be visible as a valid continuation inside while bodies.
        => AssertExpectedContains("while true { println('x')", TokenKind.BraceRight);
    
    // FOR + RANGE

    [Fact]
    public void Predict_for_expects_identifier()
        => AssertExpectedExactly("for", TokenKind.Identifier);

    [Fact]
    public void Predict_for_identifier_expects_in()
        => AssertExpectedExactly("for i", TokenKind.In);

    [Fact]
    public void Predict_for_range_expects_by_or_lbrace()
        => AssertExpectedExactly("for i in 0..10", TokenKind.By,
            TokenKind.BraceLeft,
            TokenKind.Dot,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.As);

    [Fact]
    public void Predict_for_inclusive_range_expects_by_or_lbrace()
        => AssertExpectedExactly("for i in 0..=10", TokenKind.By,
            TokenKind.BraceLeft,
            TokenKind.Dot,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.Dot,
            TokenKind.As);

    [Fact]
    public void Predict_for_range_with_step_expects_lbrace()
        => AssertExpectedExactly("for i in 0..10 by 2", TokenKind.BraceLeft,
            TokenKind.Dot,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.Dot,
            TokenKind.As);
    
    [Fact]
    public void Predict_for_body_after_expression_stmt_contains_rbrace()
        // Regression: closing brace must be visible as a valid continuation inside for bodies.
        => AssertExpectedContains("for i in 0..10 { println('x')", TokenKind.BraceRight);
    
    // JUMPS + RETURN

    [Fact]
    public void Predict_break_expects_semicolon()
        => AssertExpectedContains("break", TokenKind.Semicolon, TokenKind.VirtualTerminator);

    [Fact]
    public void Predict_continue_expects_semicolon()
        => AssertExpectedContains("continue", TokenKind.Semicolon, TokenKind.VirtualTerminator);

    [Fact]
    public void Predict_return_value_expects_semicolon()
        => AssertExpectedContains("return 1", TokenKind.Semicolon, TokenKind.VirtualTerminator);
    
    // EXPRESSION STATEMENTS

    [Fact]
    public void Predict_expression_stmt_identifier_expects_semicolon()
        => AssertExpectedContains("x", TokenKind.Semicolon, TokenKind.VirtualTerminator);

    [Fact]
    public void Predict_assignment_stmt_expects_semicolon()
        => AssertExpectedContains("x = 1", TokenKind.Semicolon, TokenKind.VirtualTerminator);
    
    // POSTFIX: member access, conversion, grouping, calls

    [Fact]
    public void Predict_member_access_dot_expects_identifier()
        => AssertExpectedExactly("x.", TokenKind.Identifier);

    [Fact]
    public void Predict_conversion_as_expects_type_identifier()
        => AssertExpectedExactly("x as", TokenKind.Identifier);

    [Fact]
    public void Predict_conversion_as_qualified_expects_identifier_after_dot()
        => AssertExpectedExactly("x as Outer.", TokenKind.Identifier);

    [Fact]
    public void Predict_grouping_expr_expects_rparen()
        => AssertExpectedExactly("(1", TokenKind.ParenthesisRight,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.And,
            TokenKind.Or,
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Greater,
            TokenKind.Less,
            TokenKind.GreaterEqual,
            TokenKind.LessEqual,
            TokenKind.Dot,
            TokenKind.As);

    [Fact]
    public void Predict_call_args_expects_comma_or_rparen()
        // After "f(1" CompleteCall will optionally accept ',' then must Consume(')')
        => AssertExpectedExactly("f(1", 
            TokenKind.Comma, TokenKind.ParenthesisRight,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.And,
            TokenKind.Or,
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Greater,
            TokenKind.Less,
            TokenKind.GreaterEqual,
            TokenKind.LessEqual,
            TokenKind.Dot,
            TokenKind.As);

    [Fact]
    public void Predict_call_member_chain_dot_expects_identifier()
        => AssertExpectedExactly("make(1).", TokenKind.Identifier);
    
    // OBJECT INITIALIZER

    [Fact]
    public void Predict_new_typename_expects_lbrace()
        // ParseInitObject: after parsing type expr, it Consume('{')
        => AssertExpectedExactly("new A", TokenKind.BraceLeft, TokenKind.Dot);

    [Fact]
    public void Predict_new_lbrace_expects_rbrace_or_identifier()
        // After "{": either "}" to end, or identifier for a field initializer
        => AssertExpectedExactly("new A {", TokenKind.BraceRight, TokenKind.Identifier);

    [Fact]
    public void Predict_new_fieldname_expects_colon()
        => AssertExpectedExactly("new A { x", TokenKind.Colon);

    [Fact]
    public void Predict_new_after_fieldvalue_expects_comma_or_rbrace()
        => AssertExpectedExactly("new A { x: 1", TokenKind.Comma, TokenKind.BraceRight,
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.Slash,
            TokenKind.And,
            TokenKind.Or,
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Greater,
            TokenKind.Less,
            TokenKind.GreaterEqual,
            TokenKind.LessEqual,
            TokenKind.Dot,
            TokenKind.As);
}
