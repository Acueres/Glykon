using Tython;
using Tython.Model;

namespace Test
{
    public class ParserTest
    {
        [Fact]
        public void PrintStmtTest()
        {
            Token[] tokens = [new("print", 0, TokenType.Keyword), new("Hello Tython", 0, TokenType.String), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "PrintStmtTest");
            var (stmts, _) = parser.Parse();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal("print", stmts[0].Token.Value);
            Assert.NotNull(stmts[0].Expression);
            Assert.Equal("Hello Tython", stmts[0].Expression.Token.Value);
        }

        [Fact]
        public void UnaryOperatorTest()
        {
            Token[] tokens = [new("not", 0, TokenType.Symbol), new("false", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "UnaryTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Unary, ast.Type);
            Assert.Equal("not", ast.Token.Value);
            Assert.NotNull(ast.Primary);
            Assert.Equal("false", ast.Primary.Token.Value);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new("true", 0, TokenType.Keyword), new("==", 0, TokenType.Symbol), new("false", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "EqualityTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("==", ast.Token.Value);
            Assert.NotNull(ast.Primary);
            Assert.Equal("true", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("false", ast.Secondary.Token.Value);
        }

        [Fact]
        public void ComparisonTest()
        {
            Token[] tokens = [new("true", 0, TokenType.Keyword), new(">", 0, TokenType.Symbol), new("false", 0, TokenType.Keyword), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "ComparisonTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(">", ast.Token.Value);
            Assert.NotNull(ast.Primary);
            Assert.Equal("true", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("false", ast.Secondary.Token.Value);
        }

        [Fact]
        public void TermTest()
        {
            Token[] tokens = [new("2", 0, TokenType.Int), new("-", 0, TokenType.Symbol), new("3", 0, TokenType.Int), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "TermTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("-", ast.Token.Value);
            Assert.NotNull(ast.Primary);
            Assert.Equal("2", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Value);
        }

        [Fact]
        public void FactorTest()
        {
            Token[] tokens = [new("6", 0, TokenType.Int), new("/", 0, TokenType.Symbol), new("3", 0, TokenType.Int), new(";", 0, TokenType.Symbol)];
            Parser parser = new(tokens, "FactorTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal("/", ast.Token.Value);
            Assert.NotNull(ast.Primary);
            Assert.Equal("6", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Value);
        }
    }
}
