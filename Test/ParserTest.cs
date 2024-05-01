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
            Assert.Equal(ExpressionType.Unary, ast.Type);
            Assert.Equal("not", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("False", ast.Primary.Token.Lexeme);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new("True", 0, TokenType.Keyword), new("==", 0, TokenType.Symbol), new("False", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "EqualityTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("==", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("True", ast.Primary.Token.Lexeme);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("False", ast.Secondary.Token.Lexeme);
        }

        [Fact]
        public void ComparisonTest()
        {
            Token[] tokens = [new("True", 0, TokenType.Keyword), new(">", 0, TokenType.Symbol), new("False", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "ComparisonTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(">", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("True", ast.Primary.Token.Lexeme);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("False", ast.Secondary.Token.Lexeme);
        }

        [Fact]
        public void TermTest()
        {
            Token[] tokens = [new("2", 0, TokenType.Int), new("-", 0, TokenType.Symbol), new("3", 0, TokenType.Int), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "TermTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("-", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("2", ast.Primary.Token.Lexeme);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Lexeme);
        }

        [Fact]
        public void FactorTest()
        {
            Token[] tokens = [new("6", 0, TokenType.Int), new("/", 0, TokenType.Symbol), new("3", 0, TokenType.Int), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "FactorTest");
            var (ast, _) = parser.Parse();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("/", ast.Token.Lexeme);
            Assert.NotNull(ast.Primary);
            Assert.Equal("6", ast.Primary.Token.Lexeme);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Lexeme);
        }
    }
}
