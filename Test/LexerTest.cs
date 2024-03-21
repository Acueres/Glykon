using Tython;
using Tython.Model;

namespace Test
{
    public class LexerTest
    {
        [Fact]
        public void ScanWithCommentsTest()
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
            const string stringsSource = "\"some text\" 'other text' \"unterminated";

            Lexer lexer = new(stringsSource, "StringTest");
            var tokens = lexer.ScanSource();

            Assert.Equal(2, tokens.Count);
            Assert.Equal(TokenType.String, tokens[0].Type);
            Assert.Equal("some text", tokens[0].Lexeme);
            Assert.Equal("other text", tokens[1].Lexeme);
        }
    }
}