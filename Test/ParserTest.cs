using Tython;
using Tython.Enum;
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
            var (stmts, _, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Print, stmts.First().Type);
            Assert.Equal(TokenType.Print, stmts.First().Token.Type);
            Assert.NotNull(stmts[0].Expression);
            Assert.Equal("Hello Tython", (stmts.First().Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void VariableDeclarationStmtTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new(42L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableDeclarationStmtTest");
            var (stmts, _, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts.First().Type);
            Assert.Equal(TokenType.Identifier, stmts.First().Token.Type);
            Assert.NotNull(stmts.First().Expression);
            Assert.Equal(42L, (stmts.First().Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void UnaryOperatorTest()
        {
            Token[] tokens = [new(TokenType.Not, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Unary, ast.Type);

            var unary = (UnaryExpr)ast;
            Assert.Equal(TokenType.Not, unary.Operator.Type);
            Assert.NotNull(unary.Expr);
            Assert.Equal(TokenType.False, (unary.Expr as LiteralExpr).Token.Type);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new(TokenType.True, 0), new(TokenType.Equal, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "EqualityTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Equal, binary.Operator.Type);
            Assert.NotNull(binary.Left);
            Assert.Equal(TokenType.True, (binary.Left as LiteralExpr).Token.Type);
            Assert.NotNull(binary.Right);
            Assert.Equal(TokenType.False, (binary.Right as LiteralExpr).Token.Type);
        }

        [Fact]
        public void ComparisonTest()
        {
            Token[] tokens = [new(2L, 0, TokenType.Int), new(TokenType.Greater, 0), new(1L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "ComparisonTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Greater, binary.Operator.Type);
            Assert.NotNull(binary.Left);
            Assert.Equal(2L, (binary.Left as LiteralExpr).Token.Value);
            Assert.NotNull(binary.Right);
            Assert.Equal(1L, (binary.Right as LiteralExpr).Token.Value);
        }

        [Fact]
        public void TermTest()
        {
            Token[] tokens = [new(2L, 0, TokenType.Int), new(TokenType.Minus, 0), new(3L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "TermTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Minus, binary.Operator.Type);
            Assert.NotNull(binary.Left);
            Assert.Equal(2L, (binary.Left as LiteralExpr).Token.Value);
            Assert.NotNull(binary.Right);
            Assert.Equal(3L, (binary.Right as LiteralExpr).Token.Value);
        }

        [Fact]
        public void FactorTest()
        {
            Token[] tokens = [new(6L, 0, TokenType.Int), new(TokenType.Slash, 0), new(3L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "FactorTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Slash, binary.Operator.Type);
            Assert.NotNull(binary.Left);
            Assert.Equal(6L, (binary.Left as LiteralExpr).Token.Value);
            Assert.NotNull(binary.Right);
            Assert.Equal(3L, (binary.Right as LiteralExpr).Token.Value);
        }
    }
}
