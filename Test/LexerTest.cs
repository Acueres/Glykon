using Tython;

namespace Test
{
    public class LexerTest
    {
        [Fact]
        public void RemoveCommentsTest()
        {
            const string commentsSource = @"
            #comment1

            code1

            #comment2

            code2 #comment3
            code3
";
            Lexer lexer = new(commentsSource, "CommentsTest");
            var tokens = lexer.ScanSource();
            Assert.Equal(3, tokens.Count);
        }
    }
}