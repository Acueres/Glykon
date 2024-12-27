using System.ComponentModel;
using System.Xml.Serialization;
using Tython.Component;
using Tython.Model;

namespace Test
{
    public class ParserTest
    {
        [Fact]
        public void BlockStatementTest()
        {
            const string fileName = "BlockStatementTest";
            const string src = @"
            let i = 6
            {
                let i = 5
            }
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, _, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.NotEmpty(stmts);
            Assert.Equal(2, stmts.Length);
            Assert.Equal(StatementType.Block, stmts[1].Type);
            BlockStmt stmt = (BlockStmt)stmts[1];
            Assert.Equal(1, stmt.Statements.Count);
        }

        [Fact]
        public void PrintStmtTest()
        {
            Token[] tokens = [new(TokenType.Print, 0), new("Hello Tython", 0, TokenType.String), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "PrintStmtTest");
            var (stmts, _, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Print, stmts.First().Type);
            PrintStmt stmt = (PrintStmt)stmts.First();
            Assert.NotNull(stmt.Expression);
            Assert.Equal("Hello Tython", (stmt.Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void VariableDeclarationTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier), new(TokenType.Assignment, 0), new(42L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableDeclarationTest");
            var (stmts, _, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts.First().Type);
            VariableStmt stmt = (VariableStmt)stmts.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenType.Int, stmt.VariableType);
            Assert.Equal(42L, (stmt.Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void VariableTypeDeclarationTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new("value", 0, TokenType.Identifier),
                new(TokenType.Colon, 0), new(TokenType.Int, 0),
                new(TokenType.Assignment, 0), new(42L, 0, TokenType.Int), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableTypeDeclarationTest");
            var (stmts, _, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts.First().Type);
            VariableStmt stmt = (VariableStmt)stmts.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenType.Int, stmt.VariableType);
            Assert.Equal(42L, (stmt.Expression as LiteralExpr).Token.Value);
        }

        [Fact]
        public void VariableTypeInferenceTest()
        {
            const string fileName = "VariableTypeInferenceTest";
            const string src = @"
            let i = 6
            let res = i + (2 + 2 * 3)
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, _, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.NotEmpty(stmts);
            Assert.Equal(2, stmts.Length);
            Assert.Equal(StatementType.Variable, stmts[1].Type);
            VariableStmt stmt = (VariableStmt)stmts[1];
            Assert.Equal("res", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenType.Int, stmt.VariableType);
        }

        [Fact]
        public void VariableWrongTypeInferenceTest()
        {
            const string fileName = "VariableWrongTypeInferenceTest";
            const string src = @"
            let res = (2 + 2 * 'text')
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, _, errors) = parser.Execute();

            Assert.Empty(stmts);
            Assert.Single(errors);
        }
        
        [Fact]
        public void AssignmentTest()
        {
            const string fileName = "AssignmentTest";
            const string src = @"
            let a = 5
            a = 3
            a = 'string ' + 'test'
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, _, errors) = parser.Execute();

            Assert.Single(errors);
            Assert.Equal(3, stmts.Length);
            Assert.Equal(ExpressionType.Assignment, stmts[1].Expression.Type);
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
