using Glykon.Compiler.Core;
using Glykon.Compiler.Syntax;

namespace Tests;

public class LexerTests
{
    [Fact]
    public void ScanIdentifiers()
    {
        string filename = nameof(ScanIdentifiers);
        const string src = @"let text = 'Hello Glykon';";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Kind != TokenKind.EOF)];

        Assert.Equal(5, tokens.Length);
        Assert.Equal(TokenKind.Let, tokens[0].Kind);
        Assert.Equal(TokenKind.Identifier, tokens[1].Kind);
        Assert.Equal("text", tokens[1].Text);
    }

    [Fact]
    public void ScanComments()
    {
        string filename = nameof(ScanComments);
        const string src = @"
            #comment1

            code1

            #comment2

            code2; #test explicit statement terminator
            code3

";
        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        //three identifiers, three statement terminators and EOF
        Assert.Equal(3 * 2 + 1, tokens.Length);
    }

    [Fact]
    public void ScanSymbols()
    {
        string filename = nameof(ScanSymbols);
        const string src = "(( )){} *+-/=<> <= == != >= // ** , . ->";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Kind != TokenKind.EOF)];

        Assert.Equal(22, tokens.Length);

        TokenKind[] expectedTypes =
        [
            TokenKind.ParenthesisLeft,
        TokenKind.ParenthesisLeft,
        TokenKind.ParenthesisRight,
        TokenKind.ParenthesisRight,
        TokenKind.BraceLeft,
        TokenKind.BraceRight,
        TokenKind.Star,
        TokenKind.Plus,
        TokenKind.Minus,
        TokenKind.Slash,
        TokenKind.Assignment,
        TokenKind.Less,
        TokenKind.Greater,
        TokenKind.LessEqual,
        TokenKind.Equal,
        TokenKind.NotEqual,
        TokenKind.GreaterEqual,
        TokenKind.SlashDouble,
        TokenKind.StarDouble,
        TokenKind.Comma,
        TokenKind.Dot,
        TokenKind.Arrow
        ];

        var actualTypes = tokens.Select(t => t.Kind).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void ScanKeywords()
    {
        string filename = nameof(ScanKeywords);
        const string src = "if class struct else def int while false";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Kind != TokenKind.EOF && t.Kind != TokenKind.Semicolon)];

        TokenKind[] expectedTypes =
        [
            TokenKind.If,
            TokenKind.Class,
            TokenKind.Struct,
            TokenKind.Else,
            TokenKind.Def,
            TokenKind.Int,
            TokenKind.While,
            TokenKind.LiteralFalse
        ];

        var actualTypes = tokens.Select(t => t.Kind).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void ScanString()
    {
        var filename = nameof(ScanString);
        const string src = "\"some text\" 'other text' \"\"\"multiline oneliner\"\"\" \"unterminated";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Kind != TokenKind.EOF && t.Kind != TokenKind.Semicolon)];

        Assert.Equal(3, tokens.Length);
        Assert.Equal(TokenKind.LiteralString, tokens[0].Kind);
        Assert.Equal("some text", tokens[0].Text);
        Assert.Equal("other text", tokens[1].Text);
        Assert.Equal("multiline oneliner", tokens[2].Text);
    }

    [Fact]
    public void ScanMultilineString()
    {
        var filename = nameof(ScanMultilineString);
        const string src = @"'''multiline string
'''
            'regular string'

            '''another 'multiline' string
 text
'''
            '''unterminated";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        //filter out statement terminators
        tokens = [.. tokens.Where(t => t.Kind != TokenKind.Semicolon && t.Kind != TokenKind.EOF)];

        Assert.Equal(3, tokens.Length);
        Assert.Equal("multiline string\n", (tokens.First().Text).Replace("\r", string.Empty));
        Assert.Equal("regular string", tokens[1].Text);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal("another 'multiline' string\n text\n", (tokens[2].Text).Replace("\r", string.Empty));
        Assert.Equal(4, tokens[2].Line);
    }

    [Fact]
    public void ScanNumbers()
    {
        string filename = nameof(ScanNumbers);
        const string src = "123 42 1.2 .2 2.";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Kind != TokenKind.EOF)];

        Assert.Equal(6, tokens.Length);
        Assert.Equal(TokenKind.LiteralInt, tokens[0].Kind);
        Assert.Equal("123", tokens[0].Text);
        Assert.Equal("42", tokens[1].Text);

        Assert.Equal(TokenKind.LiteralReal, tokens[2].Kind);
        Assert.Equal("1.2", tokens[2].Text);
        Assert.Equal(".2", tokens[3].Text);

        Assert.Equal(TokenKind.LiteralInt, tokens[4].Kind);
    }

    [Fact]
    public void SemicolonInsertion()
    {
        string filename = nameof(SemicolonInsertion);
        const string src = @"
# 1. Basic insertion
let a = 1
let b = 2

# 2. No insertion after an operator
b = a +
    2

# 3. No insertion after an opening brace or comma
def func(a,
                  b) {
    return a
}

# 4. Correct handling of explicit semicolons
let c = 3; let d = 4;

# 5. No insertion after control flow keywords
if a > b
{
    d = 5
}
";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        TokenKind[] expectedTypes =
        [
        // let a = 1;
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,
        // let b = 2;
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,
        // b = a + 2; (newline after + is ignored)
        TokenKind.Identifier, TokenKind.Assignment, TokenKind.Identifier, TokenKind.Plus, TokenKind.LiteralInt, TokenKind.Semicolon,
        // def func(a, b) { return a; } (newlines after def, comma, {, }, and before } are ignored or handled)
        TokenKind.Def, TokenKind.Identifier,
        TokenKind.ParenthesisLeft, TokenKind.Identifier, TokenKind.Comma, TokenKind.Identifier, TokenKind.ParenthesisRight,
        TokenKind.BraceLeft,
        TokenKind.Return, TokenKind.Identifier, TokenKind.Semicolon,
        TokenKind.BraceRight,
        // let c = 3;
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,
        // let d = 4;
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,
        // if a > b { d = 5; }
        TokenKind.If, TokenKind.Identifier, TokenKind.Greater, TokenKind.Identifier, TokenKind.Semicolon,
        TokenKind.BraceLeft,
        TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,
        TokenKind.BraceRight,
        // Final EOF
        TokenKind.EOF
    ];

        var actualTypes = tokens.Select(t => t.Kind).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void ChainingAndLineContinuation()
    {
        string filename = nameof(ChainingAndLineContinuation);
        const string src = @"
# Test 1: Method chaining
let item = my_collection
    .get_name()

# Test 2: Arithmetic continuation
let result = 100
    + 20

# Test 3: Logical 'and' keyword continuation
if user.is_valid
    and user.has_permission
{

}

# Test 4: Logical 'or' keyword continuation
if user.is_guest
    or user.is_new
{

}

# Test 5: Negative test for 'and'. Semicolon MUST be inserted.
let name = ""andre""
let id = 1

# Test 6: Negative test for 'or'. Semicolon MUST be inserted.
let status = ""order""
let details = ""...""
";

        SourceText source = new(filename, src);
        Lexer lexer = new(source, filename);
        var (tokens, _) = lexer.Execute();

        var expectedTypes = new[]
        {
        // Test 1: `let item = my_collection.get_name()`
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.Identifier, TokenKind.Dot,
        TokenKind.Identifier, TokenKind.ParenthesisLeft, TokenKind.ParenthesisRight, TokenKind.Semicolon,
        
        // Test 2: `let result = 100 + 20;`
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Plus,
        TokenKind.LiteralInt, TokenKind.Semicolon,

        // Test 3: `if user.is_valid and user.has_permission; { }`
        TokenKind.If, TokenKind.Identifier, TokenKind.Dot, TokenKind.Identifier, TokenKind.And,
        TokenKind.Identifier, TokenKind.Dot, TokenKind.Identifier, TokenKind.Semicolon, TokenKind.BraceLeft, TokenKind.BraceRight,

        // Test 4: `if user.is_guest or user.is_new; { }`
        TokenKind.If, TokenKind.Identifier, TokenKind.Dot, TokenKind.Identifier, TokenKind.Or,
        TokenKind.Identifier, TokenKind.Dot, TokenKind.Identifier, TokenKind.Semicolon, TokenKind.BraceLeft, TokenKind.BraceRight,

        // Test 5: `let name = "andre";` and `let id = 1;`
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralString, TokenKind.Semicolon,
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralInt, TokenKind.Semicolon,

        // Test 6: `let status = "order";` and `let details = "...";`
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralString, TokenKind.Semicolon,
        TokenKind.Let, TokenKind.Identifier, TokenKind.Assignment, TokenKind.LiteralString, TokenKind.Semicolon,

        TokenKind.EOF
    };

        var actualTypes = tokens.Select(t => t.Kind).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }
}