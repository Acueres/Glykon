using Tython;
using Tython.Enum;
using Tython.Model;

namespace Test
{
    public class OptimizerTest
    {
        [Fact]
        public void TestBinaryExpressionLiteralOptimization()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new(2L, 0, TokenType.Int), new(TokenType.Star, 0),  new(3L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "BinaryLiteralOptimizationTest");
            var (stmts, _, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Type);
            Assert.Equal(6L, (optimizedStmts.First().Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void TestUnaryExpressionLiteralOptimization()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new(TokenType.Not, 0), new(TokenType.False, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryLiteralOptimizationTest");
            var (stmts, _, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Literal, optimizedStmts.First().Expression.Type);
            Assert.Equal(TokenType.True, (optimizedStmts.First().Expression as LiteralExpr).Token.Type);
        }

        [Fact]
        public void TestBinaryExpressionWithIdentifierLiteralOptimization()
        {
            const string src = @"
            let value: int = 100
            let result = (value * 2) / (2 + 2 * 3)
";
            const string name = "BinaryWithIdentifierLiteralOptimizationTest";
            Lexer lexer = new(src, name);
            var (tokens, _) = lexer.Execute();

            Parser parser = new(tokens, name);
            var (stmts, _, _) = parser.Execute();

            Optimizer optimizer = new(stmts);
            var optimizedStmts = optimizer.Execute();

            var division = (BinaryExpr)optimizedStmts[1].Expression;
            var left = (BinaryExpr)division.Left;
            var valueVar = (VariableExpr)left.Left;
            var leftTwoConst = (LiteralExpr)left.Right;
            var right = (LiteralExpr)division.Right;

            Assert.NotEmpty(optimizedStmts);
            Assert.Equal(ExpressionType.Binary, division.Type);
            Assert.Equal(ExpressionType.Binary, left.Type);
            Assert.Equal(ExpressionType.Variable, valueVar.Type);
            Assert.Equal(ExpressionType.Literal, leftTwoConst.Type);
            Assert.Equal(ExpressionType.Literal, right.Type);
            Assert.Equal(8L, right.Token.Value);
        }
    }
}
