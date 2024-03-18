using Tython;

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
            const string symbols = "(( )){} *+-/=<> <= == >= // ** , .";

            Lexer lexer = new(symbols, "CommentsTest");
            var tokens = lexer.ScanSource();
            Assert.Equal(20, tokens.Count);
        }
    }
}