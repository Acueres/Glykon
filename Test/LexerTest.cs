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
            #comment2
            
            ##multiline
            comment
            test##
";
            Lexer lexer = new(commentsSource, "CommentsTest");
            var tokens = lexer.ScanSource();
            Assert.Empty(tokens);
        }
    }
}