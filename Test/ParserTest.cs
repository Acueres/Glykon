using Tython;
using Tython.Model;

namespace Test
{
    public class ParserTest
    {
        [Fact]
        public void UnaryOperatorTest()
        {
            Token[] tokens = [new("not", 0, TokenType.Symbol), new("False", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "UnaryTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal("not", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("False", ast.Primary.Token.Lexeme);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new("a", 0, TokenType.Identifier), new("==", 0, TokenType.Symbol), new("b", 0, TokenType.Identifier), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "EqualityTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal("==", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("a", ast.Primary.Token.Lexeme);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("b", ast.Secondary.Token.Lexeme);
        }
    }
}
