using Tython;
using Tython.Model;

namespace Test
{
    public class LexerTest
    {
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
            var tokens = lexer.ScanSource();

            //three identifiers and three statement terminators
            Assert.Equal(3 * 2, tokens.Count);
        }

        [Fact]
        public void ScanSymbolsTest()
        {
            const string symbols = "(( )){} *+-/=<> <= == != >= // ** , .";

            Lexer lexer = new(symbols, "SymbolsTest");
            var tokens = lexer.ScanSource();

            Assert.Equal(21, tokens.Count);
        }

        [Fact]
        public void ScanStringTest()
        {
            const string stringsSource = "\"some text\" 'other text' \"\"\"multiline oneliner\"\"\" \"unterminated";

            Lexer lexer = new(stringsSource, "StringTest");
            var tokens = lexer.ScanSource();

            Assert.Equal(3, tokens.Count);
            Assert.Equal(TokenType.String, tokens[0].Type);
            Assert.Equal("some text", tokens[0].Lexeme);
            Assert.Equal("other text", tokens[1].Lexeme);
            Assert.Equal("multiline oneliner", tokens[2].Lexeme);
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
            var tokens = lexer.ScanSource();

            //filter out statement terminators
            tokens = tokens.Where(t => t.Type != TokenType.Symbol).ToList();

            Assert.Equal(3, tokens.Count);
            Assert.Equal("multiline string\r\n", tokens[0].Lexeme);
            Assert.Equal("regular string", tokens[1].Lexeme);
            Assert.Equal(2, tokens[1].Line);
            Assert.Equal("another 'multiline' string\r\n text\r\n", tokens[2].Lexeme);
            Assert.Equal(4, tokens[2].Line);
        }

        [Fact]
        public void ScanNumbersTest()
        {
            const string numbers = "123 42 1.2 .2 2.";

            Lexer lexer = new(numbers, "NumbersTest");
            var tokens = lexer.ScanSource();

            Assert.Equal(6, tokens.Count);
            Assert.Equal(TokenType.Int, tokens[0].Type);
            Assert.Equal("123", tokens[0].Lexeme);
            Assert.Equal("42", tokens[1].Lexeme);

            Assert.Equal(TokenType.Float, tokens[2].Type);
            Assert.Equal("1.2", tokens[2].Lexeme);
            Assert.Equal(".2", tokens[3].Lexeme);

            Assert.Equal(TokenType.Int, tokens[4].Type);
        }
    }
}