using Tython;
using Tython.Model;

namespace Test
{
    public class OptimizerTest
    {
        [Fact]
        public void TestBinaryExpressionLiteralOptimization()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new("2", 0, TokenType.Int), new(TokenType.Star, 0),  new("3", 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "BinaryLiteralOptimizationTest");
            var (stmts, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Type);
            Assert.Equal("6", optimizedStmts.First().Expression.Token.Value);
        }

        [Fact]
        public void TestUnaryExpressionLiteralOptimization()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new(TokenType.Not, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryLiteralOptimizationTest");
            var (stmts, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Type);
            Assert.Equal(TokenType.True, optimizedStmts.First().Expression.Token.Type);
        }

        [Fact]
        public void TestBinaryExpressionWithIdentifierLiteralOptimization()
        {
            const string src = @"
            let result = (value * 2) / (2 + 2 * 3)
";
            const string name = "BinaryWithIdentifierLiteralOptimizationTest";
            Lexer lexer = new(src, name);
            var (tokens, _) = lexer.Execute();

            Parser parser = new(tokens, name);
            var (stmts, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Binary, optimizedStmts.First().Expression.Type);
            Assert.Equal(ExpressionType.Binary, optimizedStmts.First().Expression.Primary.Type);
            Assert.Equal(ExpressionType.Variable, optimizedStmts.First().Expression.Primary.Primary.Type);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Primary.Secondary.Type);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Secondary.Type);
        }
    }
}
