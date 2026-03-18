using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax;
using Tests.Infrastructure;

namespace Tests;

public sealed class ParserPredictExpectedTokensTests : CompilerTestBase
{
    private static HashSet<TokenKind> Set(params TokenKind[] kinds) => kinds.ToHashSet();

    private void AssertExpectedExactly(string src, params TokenKind[] expectedKinds)
    {
        var (_, _, lexErrors, parseErrors, expected, _, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        Assert.Equal(Set(expectedKinds), actual);
    }

    private void AssertExpectedContains(string src, params TokenKind[] mustContain)
    {
        var (_, _, lexErrors, parseErrors, expected, _, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        foreach (var k in mustContain)
            Assert.Contains(k, actual);
    }

    private void AssertExpectedNotContains(string src, params TokenKind[] mustNotContain)
    {
        var (_, _, lexErrors, parseErrors, expected, _, _) = Parse(src, SyntaxMode.Predict);

        Assert.Empty(lexErrors);
        Assert.Empty(parseErrors);

        var actual = (expected ?? Enumerable.Empty<TokenKind>()).ToHashSet();
        foreach (var k in mustNotContain)
        {
            Assert.DoesNotContain(k, actual);
        }
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
    public void Predict_let_expects_assignment()
        => AssertExpectedContains("let x", TokenKind.Assignment);

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
    
    [Fact]
    public void Predict_block_after_expr_same_line_allows_rbrace_but_not_new_statement()
    {
        AssertExpectedContains("def main() { func ", TokenKind.BraceRight);
        AssertExpectedNotContains("def main() { func ", TokenKind.Identifier, TokenKind.Let, TokenKind.Def);
    }
    
    [Fact]
    public void Predict_in_block_after_explicit_semicolon_same_line_allows_new_statement_start()
    {
        // Explicit ';' terminates the statement even on the same line, so a new statement may start.
        AssertExpectedContains(
            "def main() { x = 1;",
            TokenKind.Identifier, TokenKind.Let, TokenKind.Return, TokenKind.BraceRight
        );
    }

    [Fact]
    public void Predict_in_block_after_explicit_semicolon_same_line_does_not_require_newline_for_statement_start()
    {
        AssertExpectedContains(
            "def add(x: real, y: real) -> real { let sum: real = x + y;",
            TokenKind.Identifier, TokenKind.Let, TokenKind.Return, TokenKind.BraceRight
        );
    }

    [Fact]
    public void Predict_in_block_after_explicit_semicolon_newline_allows_new_statement_start()
    {
        // After ';' and newline, statement start should obviously be allowed too.
        AssertExpectedContains(
            "def main() { x = 1;\n",
            TokenKind.Identifier, TokenKind.Let, TokenKind.Return
        );

        AssertExpectedContains(
            "def main() { x = 1;\n",
            TokenKind.BraceRight
        );
    }

    [Fact]
    public void Predict_in_block_same_line_after_expression_without_semicolon_does_not_allow_new_statement_start()
    {
        // Control test: without ';' (and no newline), statement-start must not leak.
        AssertExpectedNotContains(
            "def main() { x = 1",
            TokenKind.Identifier, TokenKind.Let, TokenKind.Def, TokenKind.Const
        );

        // But closing the block should still be possible.
        AssertExpectedContains(
            "def main() { x = 1",
            TokenKind.BraceRight
        );
    }
    
    [Fact]
    public void Predict_function_body_start_allows_statement_starts_and_rbrace()
    {
        AssertExpectedContains(
            "def add(x: real, y: real) -> real {",
            TokenKind.BraceRight,
            TokenKind.Identifier,
            TokenKind.Let,
            TokenKind.Return,
            TokenKind.If,
            TokenKind.While,
            TokenKind.For,
            TokenKind.Break,
            TokenKind.Continue,
            TokenKind.BraceLeft
        );
    }
    
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

    [Fact]
    public void Predict_while_condition_contains_lbrace()
        => AssertExpectedContains("while true", TokenKind.BraceLeft);
    
    [Fact]
    public void Predict_while_after_condition_expects_lbrace()
        => AssertExpectedContains("while i <= 10", TokenKind.BraceLeft);
    
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
    public void Predict_for_after_in_allows_range_start_expression()
    {
        AssertExpectedContains(
            "for i in",
            TokenKind.Identifier,
            TokenKind.LiteralInt,
            TokenKind.LiteralReal,
            TokenKind.ParenthesisLeft
        );
    }
    
    [Fact]
    public void Predict_for_after_first_range_bound_expects_range_operator()
        => AssertExpectedContains("for i in 0", TokenKind.Range, TokenKind.RangeInclusive);

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
        => AssertExpectedContains("break", TokenKind.Semicolon);

    [Fact]
    public void Predict_continue_expects_semicolon()
        => AssertExpectedContains("continue", TokenKind.Semicolon);

    [Fact]
    public void Predict_return_value_expects_semicolon()
        => AssertExpectedContains("return 1", TokenKind.Semicolon);
    
    // EXPRESSION STATEMENTS

    [Fact]
    public void Predict_expression_stmt_identifier_expects_semicolon()
        => AssertExpectedContains("x", TokenKind.Semicolon);

    [Fact]
    public void Predict_assignment_stmt_does_not_allow_new_statement_start()
        => AssertExpectedNotContains("x = 1", TokenKind.Identifier, TokenKind.Let, TokenKind.Def, TokenKind.Const);
    
    [Fact]
    public void Predict_assignment_stmt_newline_allows_new_statement_start()
    {
        // newline => ASI possible => next statement starters should appear
        AssertExpectedContains("x = 1\n", TokenKind.Identifier, TokenKind.Let, TokenKind.Def,
            TokenKind.Const);
    }

    [Fact]
    public void Predict_in_block_same_line_after_stmt_allows_rbrace_but_not_new_statement()
    {
        // After a statement at end-of-input inside a block, the next likely token is '}'
        // but a new statement must not start on the same line without newline/semicolon.
        AssertExpectedContains("def main() { x = 1", TokenKind.BraceRight);
        AssertExpectedNotContains("def main() { x = 1", TokenKind.Identifier, TokenKind.Let, TokenKind.Def, TokenKind.Const);
    }

    [Fact]
    public void Predict_block_start_same_line_allows_statement_or_decl_start()
    {
        AssertExpectedContains("def add(x: int, y: int) -> int {", TokenKind.BraceRight, TokenKind.Let,
            TokenKind.Return, TokenKind.Identifier, TokenKind.Def, TokenKind.Const);
    }

    [Fact]
    public void Predict_in_block_after_newline_allows_new_statement_start()
    {
        // newline inside block => statement starters should appear
        AssertExpectedContains("def main() { x = 1\n", TokenKind.Identifier, TokenKind.Let,
            TokenKind.Def, TokenKind.Const);
    }
    
    [Fact]
    public void Predict_same_line_after_identifier_does_not_allow_new_statement_start()
    {
        // "func " ends with identifier + spaces; no newline => must NOT allow starting a new statement
        AssertExpectedNotContains("func ", TokenKind.Identifier, TokenKind.Let, TokenKind.Def, TokenKind.Const);
    }
    
    [Fact]
    public void Predict_after_newline_allows_new_statement_start()
    {
        AssertExpectedContains("func\n", TokenKind.Identifier, TokenKind.Let, TokenKind.Def);
    }
    
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
        => AssertExpectedContains("f(1", 
            TokenKind.Comma, TokenKind.ParenthesisRight);

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
    
    [Fact]
    public void Predict_empty_function_body_allows_statement_starts()
        => AssertExpectedContains(
            "def print_numbers(i: int) {",
            TokenKind.BraceRight,
            TokenKind.Let,
            TokenKind.Return,
            TokenKind.For,
            TokenKind.Identifier,
            TokenKind.If,
            TokenKind.While
        );

    [Fact]
    public void Predict_for_range_start_allows_term_starts()
        => AssertExpectedContains(
            "def print_numbers(i: int) { for j in ",
            TokenKind.Identifier,
            TokenKind.LiteralInt,
            TokenKind.LiteralReal,
            TokenKind.ParenthesisLeft,
            TokenKind.Minus,
            TokenKind.New
        );

    [Fact]
    public void Predict_after_first_range_bound_expects_range_operator()
        => AssertExpectedContains(
            "def print_numbers(i: int) { for j in 0",
            TokenKind.Range,
            TokenKind.RangeInclusive
        );

    [Fact]
    public void Predict_after_first_range_bound_does_not_allow_block_or_step_yet()
        => AssertExpectedNotContains(
            "def print_numbers(i: int) { for j in 0",
            TokenKind.By,
            TokenKind.BraceLeft
        );

    [Fact]
    public void Predict_after_complete_range_allows_by_or_block()
        => AssertExpectedContains(
            "def print_numbers(i: int) { for j in 0..10",
            TokenKind.By,
            TokenKind.BraceLeft
        );
    
    [Fact]
    public void Predict_initializer_expression_start_allows_new()
        => AssertExpectedContains(
            "let point = ",
            TokenKind.New,
            TokenKind.Identifier,
            TokenKind.LiteralInt,
            TokenKind.LiteralReal,
            TokenKind.ParenthesisLeft
        );

    [Fact]
    public void Predict_after_new_expects_type_name()
        => AssertExpectedContains(
            "let point = new ",
            TokenKind.Identifier
        );
    
    [Fact]
    public void Predict_after_function_name_paren_allows_parameter_or_close()
        => AssertExpectedContains(
            "def sum_to_ten(",
            TokenKind.Identifier,
            TokenKind.ParenthesisRight
        );

    [Fact]
    public void Predict_after_function_name_paren_does_not_allow_declaration_keywords()
        => AssertExpectedNotContains(
            "def sum_to_ten(",
            TokenKind.Const,
            TokenKind.Let,
            TokenKind.Def,
            TokenKind.BraceLeft
        );

    [Fact]
    public void Predict_after_empty_parameter_list_allows_arrow_or_body()
        => AssertExpectedContains(
            "def sum_to_ten()",
            TokenKind.Arrow,
            TokenKind.BraceLeft
        );
    
    [Fact]
    public void Predict_after_as_expects_type_identifier()
        => AssertExpectedContains(
            "println(j as ",
            TokenKind.Identifier
        );
    
    [Fact]
    public void Predict_after_while_block_return_available()
        => AssertExpectedContains(
            """
            def sum_to_ten( ) -> int {
                    let sum: int = 0;
                    let i: int = 1;
                    while i <= 10 {
                        sum = sum + i;
                        i = i + 1;
                    }
            """,
            TokenKind.Return
        );
    
    [Fact]
    public void Predict_after_expression_statement_statements_available()
        => AssertExpectedContains(
            """
            def main( ) {
                let result: int = add(1, 2)
                
            """,
            TokenKind.Identifier
        );
}
