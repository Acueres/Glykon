using TythonCompiler.Tokenization;

namespace Test;

public class LexerTest
{
    [Fact]
    public void ScanIdentifiersTest()
    {
        const string source = @"let text = 'Hello Tython';";

        Lexer lexer = new(source, "IdentifiersTest");
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Type != TokenType.EOF)];

        Assert.Equal(5, tokens.Length);
        Assert.Equal(TokenType.Let, tokens[0].Type);
        Assert.Equal(TokenType.Identifier, tokens[1].Type);
        Assert.Equal("text", tokens[1].Value);
    }

    [Fact]
    public void ScanCommentsTest()
    {
        const string commentsSource = @"
            #comment1

            code1

            #comment2

            code2; #test explicit statement terminator
            code3

";
        Lexer lexer = new(commentsSource, "CommentsTest");
        var (tokens, _) = lexer.Execute();

        //three identifiers, three statement terminators and EOF
        Assert.Equal(3 * 2 + 1, tokens.Length);
    }

    [Fact]
    public void ScanSymbolsTest()
    {
        const string symbols = "(( )){} *+-/=<> <= == != >= // ** , . ->";

        Lexer lexer = new(symbols, "SymbolsTest");
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Type != TokenType.EOF)];

        Assert.Equal(22, tokens.Length);

        TokenType[] expectedTypes =
        [
            TokenType.ParenthesisLeft,
        TokenType.ParenthesisLeft,
        TokenType.ParenthesisRight,
        TokenType.ParenthesisRight,
        TokenType.BraceLeft,
        TokenType.BraceRight,
        TokenType.Star,
        TokenType.Plus,
        TokenType.Minus,
        TokenType.Slash,
        TokenType.Assignment,
        TokenType.Less,
        TokenType.Greater,
        TokenType.LessEqual,
        TokenType.Equal,
        TokenType.NotEqual,
        TokenType.GreaterEqual,
        TokenType.SlashDouble,
        TokenType.StarDouble,
        TokenType.Comma,
        TokenType.Dot,
        TokenType.Arrow
        ];

        var actualTypes = tokens.Select(t => t.Type).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void ScanKeywordsTest()
    {
        const string keywords = "if class struct else def int while false";

        Lexer lexer = new(keywords, "KeywordsTest");
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Type != TokenType.EOF)];

        TokenType[] expectedTypes =
        [
        TokenType.If,
        TokenType.Class,
        TokenType.Struct,
        TokenType.Else,
        TokenType.Def,
        TokenType.Int,
        TokenType.While,
        TokenType.LiteralFalse
    ];

        var actualTypes = tokens.Select(t => t.Type).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }

    [Fact]
    public void ScanStringTest()
    {
        const string stringsSource = "\"some text\" 'other text' \"\"\"multiline oneliner\"\"\" \"unterminated";

        Lexer lexer = new(stringsSource, "StringTest");
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Type != TokenType.EOF)];

        Assert.Equal(3, tokens.Length);
        Assert.Equal(TokenType.LiteralString, tokens[0].Type);
        Assert.Equal("some text", tokens[0].Value);
        Assert.Equal("other text", tokens[1].Value);
        Assert.Equal("multiline oneliner", tokens[2].Value);
    }

    [Fact]
    public void ScanMultilineStringTest()
    {
        const string stringsSource = @"'''multiline string
'''
            'regular string'

            '''another 'multiline' string
 text
'''
            '''unterminated";

        Lexer lexer = new(stringsSource, "StringTest");
        var (tokens, _) = lexer.Execute();

        //filter out statement terminators
        tokens = [.. tokens.Where(t => t.Type != TokenType.Semicolon && t.Type != TokenType.EOF)];

        Assert.Equal(3, tokens.Length);
        Assert.Equal("multiline string\n", (tokens.First().Value as string).Replace("\r", string.Empty));
        Assert.Equal("regular string", tokens[1].Value);
        Assert.Equal(2, tokens[1].Line);
        Assert.Equal("another 'multiline' string\n text\n", (tokens[2].Value as string).Replace("\r", string.Empty));
        Assert.Equal(4, tokens[2].Line);
    }

    [Fact]
    public void ScanNumbersTest()
    {
        const string numbers = "123 42 1.2 .2 2.";

        Lexer lexer = new(numbers, "NumbersTest");
        var (tokens, _) = lexer.Execute();

        tokens = [.. tokens.Where(t => t.Type != TokenType.EOF)];

        Assert.Equal(6, tokens.Length);
        Assert.Equal(TokenType.LiteralInt, tokens[0].Type);
        Assert.Equal(123, tokens[0].Value);
        Assert.Equal(42, tokens[1].Value);

        Assert.Equal(TokenType.LiteralReal, tokens[2].Type);
        Assert.Equal(1.2, tokens[2].Value);
        Assert.Equal(.2, tokens[3].Value);

        Assert.Equal(TokenType.LiteralInt, tokens[4].Type);
    }

    [Fact]
    public void SemicolonInsertionTest()
    {
        const string source = @"
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

        Lexer lexer = new(source, "SemicolonInsertionTest");
        var (tokens, _) = lexer.Execute();

        TokenType[] expectedTypes =
        [
        // let a = 1;
        TokenType.Let, TokenType.Identifier, TokenType.Assignment, TokenType.LiteralInt, TokenType.Semicolon,
        // let b = 2;
        TokenType.Let, TokenType.Identifier, TokenType.Assignment, TokenType.LiteralInt, TokenType.Semicolon,
        // b = a + 2; (newline after + is ignored)
        TokenType.Identifier, TokenType.Assignment, TokenType.Identifier, TokenType.Plus, TokenType.LiteralInt, TokenType.Semicolon,
        // def func(a, b) { return a; }; (newlines after def, comma, {, }, and before } are ignored or handled)
        TokenType.Def, TokenType.Identifier,
        TokenType.ParenthesisLeft, TokenType.Identifier, TokenType.Comma, TokenType.Identifier, TokenType.ParenthesisRight,
        TokenType.BraceLeft,
        TokenType.Return, TokenType.Identifier, TokenType.Semicolon,
        TokenType.BraceRight, TokenType.Semicolon,
        // let c = 3;
        TokenType.Let, TokenType.Identifier, TokenType.Assignment, TokenType.LiteralInt, TokenType.Semicolon,
        // let d = 4;
        TokenType.Let, TokenType.Identifier, TokenType.Assignment, TokenType.LiteralInt, TokenType.Semicolon,
        // if a > b { d = 5; };
        TokenType.If, TokenType.Identifier, TokenType.Greater, TokenType.Identifier, TokenType.Semicolon,
        TokenType.BraceLeft,
        TokenType.Identifier, TokenType.Assignment, TokenType.LiteralInt, TokenType.Semicolon,
        TokenType.BraceRight, TokenType.Semicolon,
        // Final EOF
        TokenType.EOF
    ];

        var actualTypes = tokens.Select(t => t.Type).ToArray();

        Assert.Equal(expectedTypes, actualTypes);
    }
}