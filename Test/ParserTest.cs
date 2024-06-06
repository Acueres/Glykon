using Tython;
using Tython.Model;

namespace Test
{
    public class ParserTest
    {
        [Fact]
        public void PrintStmtTest()
        {
            Token[] tokens = [new(TokenType.Print, 0), new("Hello Tython", 0, TokenType.String), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "PrintStmtTest");
            var (stmts, _) = parser.Parse();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Print, stmts[0].Type);
            Assert.Equal(TokenType.Print, stmts[0].Token.Type);
            Assert.NotNull(stmts[0].Expression);
            Assert.Equal("Hello Tython", stmts[0].Expression.Token.Value);
        }

        [Fact]
        public void VariableDeclarationStmtTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new("42", 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableDeclarationStmtTest");
            var (stmts, _) = parser.Parse();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts[0].Type);
            Assert.Equal(TokenType.Identifier, stmts[0].Token.Type);
            Assert.NotNull(stmts[0].Expression);
            Assert.Equal("42", stmts[0].Expression.Token.Value);
        }

        [Fact]
        public void UnaryOperatorTest()
        {
            Token[] tokens = [new(TokenType.Not, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Unary, ast.Type);
            Assert.Equal(TokenType.Not, ast.Token.Type);
            Assert.NotNull(ast.Primary);
            Assert.Equal(TokenType.False, ast.Primary.Token.Type);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new(TokenType.True, 0), new(TokenType.Equal, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "EqualityTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(TokenType.Equal, ast.Token.Type);
            Assert.NotNull(ast.Primary);
            Assert.Equal(TokenType.True, ast.Primary.Token.Type);
            Assert.NotNull(ast.Secondary);
            Assert.Equal(TokenType.False, ast.Secondary.Token.Type);
        }

        [Fact]
        public void ComparisonTest()
        {
            Token[] tokens = [new("2", 0, TokenType.Int), new(TokenType.Greater, 0), new("1", 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "ComparisonTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(TokenType.Greater, ast.Token.Type);
            Assert.NotNull(ast.Primary);
            Assert.Equal("2", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("1", ast.Secondary.Token.Value);
        }

        [Fact]
        public void TermTest()
        {
            Token[] tokens = [new("2", 0, TokenType.Int), new(TokenType.Minus, 0), new("3", 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "TermTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(TokenType.Minus, ast.Token.Type);
            Assert.NotNull(ast.Primary);
            Assert.Equal("2", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Value);
        }

        [Fact]
        public void FactorTest()
        {
            Token[] tokens = [new("6", 0, TokenType.Int), new(TokenType.Slash, 0), new("3", 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "FactorTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);
            Assert.Equal(TokenType.Slash, ast.Token.Type);
            Assert.NotNull(ast.Primary);
            Assert.Equal("6", ast.Primary.Token.Value);
            Assert.NotNull(ast.Secondary);
            Assert.Equal("3", ast.Secondary.Token.Value);
        }
    }
}
