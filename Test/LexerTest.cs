using Tython.Component;
using Tython.Model;

namespace Test
{
    public class LexerTest
    {
        [Fact]
        public void ScanIdentifiersTest()
        {
            const string source = @"let text = 'Hello Tython'
                                    print text";

            Lexer lexer = new(source, "IdentifiersTest");
            var (tokens, _) = lexer.Execute();

            tokens = tokens.Where(t => t.Type != TokenType.EOF).ToArray();

            Assert.Equal(7, tokens.Length);
            Assert.Equal(TokenType.Let, tokens[0].Type);
            Assert.Equal(TokenType.Identifier, tokens[1].Type);
            Assert.Equal("text", tokens[1].Value);
            Assert.Equal(TokenType.Print, tokens[5].Type);
            Assert.Equal(TokenType.Identifier, tokens[6].Type);
            Assert.Equal("text", tokens[6].Value);
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
            const string symbols = "(( )){} *+-/=<> <= == != >= // ** , .";

            Lexer lexer = new(symbols, "SymbolsTest");
            var (tokens, _) = lexer.Execute();

            tokens = tokens.Where(t => t.Type != TokenType.EOF).ToArray();

            Assert.Equal(21, tokens.Length);
        }

        [Fact]
        public void ScanKeywordsTest()
        {
            const string keywords = "if class struct else def int while False";

            Lexer lexer = new(keywords, "KeywordsTest");
            var (tokens, _) = lexer.Execute();

            tokens = tokens.Where(t => t.Type != TokenType.EOF).ToArray();

            Assert.Equal(8, tokens.Length);
            Assert.Equal(TokenType.If, tokens[0].Type);
            Assert.Equal(TokenType.Int, tokens[5].Type);
        }

        [Fact]
        public void ScanStringTest()
        {
            const string stringsSource = "\"some text\" 'other text' \"\"\"multiline oneliner\"\"\" \"unterminated";

            Lexer lexer = new(stringsSource, "StringTest");
            var (tokens, _) = lexer.Execute();

            tokens = tokens.Where(t => t.Type != TokenType.EOF).ToArray();

            Assert.Equal(3, tokens.Length);
            Assert.Equal(TokenType.String, tokens[0].Type);
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
            tokens = tokens.Where(t => t.Type != TokenType.Semicolon && t.Type != TokenType.EOF).ToArray();

            Assert.Equal(3, tokens.Length);
            Assert.Equal("multiline string\n", (tokens[0].Value as string).Replace("\r", string.Empty));
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

            tokens = tokens.Where(t => t.Type != TokenType.EOF).ToArray();

            Assert.Equal(6, tokens.Length);  
            Assert.Equal(TokenType.Int, tokens[0].Type);
            Assert.Equal(123, tokens[0].Value);
            Assert.Equal(42, tokens[1].Value);

            Assert.Equal(TokenType.Real, tokens[2].Type);
            Assert.Equal(1.2, tokens[2].Value);
            Assert.Equal(.2, tokens[3].Value);

            Assert.Equal(TokenType.Int, tokens[4].Type);
        }
    }
}