using Glykon.Compiler.Semantics;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Tests
{
    public class ParserTests
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
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.NotEmpty(stmts);
            Assert.Equal(2, stmts.Length);
            Assert.Equal(StatementType.Block, stmts[1].Type);
            BlockStmt stmt = (BlockStmt)stmts[1];
            Assert.Single(stmt.Statements);
        }

        [Fact]
        public void IfStatementTest()
        {
            const string fileName = "IfStatementTest";
            const string src = @"
            let condition = true
            let second_condition = false
            if condition {
                let i = 0
                println(i)
            }
            elif second_condition {
                let i = 1
                println(i)
            }
            else if not second_condition {
                let i = 2
                println(i)
            }
            else {
                let i = 3
                println(i)
            }
            ";

            Lexer lexer = new(src, fileName);
            (var tokens, var lexerErrors) = lexer.Execute();

            Assert.Empty(lexerErrors);

            Parser parser = new(tokens, fileName);
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(3, stmts.Length);

            IfStmt ifStmt = (IfStmt)stmts[2];
            Assert.NotNull(ifStmt.ElseStatement);

            IfStmt elifStmt = (IfStmt)ifStmt.ElseStatement;
            Assert.NotNull(elifStmt.ElseStatement);

            IfStmt elseifStmt = (IfStmt)elifStmt.ElseStatement;
            Assert.NotNull(elseifStmt.ElseStatement);
        }

        [Fact]
        public void WhileStatementTest()
        {
            const string fileName = "WhileStatementTest";
            const string src = @"
            let condition = true
            while condition {
                println('ok')
            }
            ";

            Lexer lexer = new(src, fileName);
            (var tokens, var lexerErrors) = lexer.Execute();

            Assert.Empty(lexerErrors);

            Parser parser = new(tokens, fileName);
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(2, stmts.Length);

            WhileStmt whileStmt = (WhileStmt)stmts[1];
            Assert.NotNull(whileStmt.Statement);
            Assert.NotNull(whileStmt.Expression);
        }

        [Fact]
        public void FunctionDeclarationTest()
        {
            const string fileName = "FunctionDeclarationTest";
            const string src = @"
            def f(a: int, b: int) -> int {
                return a + b
            }
            ";

            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();

            Parser parser = new(tokens, fileName);
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Function, stmts.First().Type);

            FunctionStmt function = (FunctionStmt)stmts.First();
            Assert.Equal("f", function.Name);
            Assert.Equal(TokenType.Int, function.ReturnType);
            Assert.Equal(2, function.Parameters.Count);
            Assert.NotNull(function.Body);
            Assert.Single(function.Body.Statements);
            Assert.Equal(StatementType.Return, function.Body.Statements.Single().Type);
        }

        [Fact]
        public void ConstantDeclarationTest()
        {
            Token[] tokens = [new(TokenType.Const, 0), new(TokenType.Identifier, 0, "pi"),
            new(TokenType.Colon, 0), new(TokenType.Real, 0),
            new(TokenType.Assignment, 0), new(TokenType.LiteralReal, 0, 3.14), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "ConstantDeclarationTest");
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Constant, stmts.First().Type);

            SemanticBinder binder = new(stmts, new());
            var st = binder.Bind();

            var symbol = st.GetSymbol("pi");
            Assert.NotNull(symbol);
            Assert.True(symbol is ConstantSymbol);

            var constant = (ConstantSymbol)symbol;
            Assert.Equal(3.14, constant.Value.RealValue);
        }

        [Fact]
        public void VariableDeclarationTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new(TokenType.Identifier, 0, "value"), new(TokenType.Assignment, 0), new(TokenType.LiteralInt, 0, 42), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableDeclarationTest");
            var (stmts, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts.First().Type);
            VariableStmt stmt = (VariableStmt)stmts.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenType.None, stmt.VariableType); // Parser cannot infer types
            Assert.Equal(42, ((LiteralExpr)stmt.Expression).Token.IntValue);
        }

        [Fact]
        public void VariableTypeDeclarationTest()
        {
            Token[] tokens = [new(TokenType.Let, 0), new(TokenType.Identifier, 0, "value"),
                new(TokenType.Colon, 0), new(TokenType.Int, 0),
                new(TokenType.Assignment, 0), new(TokenType.LiteralInt, 0, 42), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "VariableTypeDeclarationTest");
            var (stmts, _) = parser.Execute();

            Assert.NotEmpty(stmts);
            Assert.Single(stmts);
            Assert.Equal(StatementType.Variable, stmts.First().Type);
            VariableStmt stmt = (VariableStmt)stmts.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenType.Int, stmt.VariableType);
            Assert.Equal(42, (stmt.Expression as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void CallTest()
        {
            const string fileName = "CallTest";
            const string src = @"
            function('call test')
            ";

            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(stmts);
            Assert.True(stmts.First().Expression.Type == ExpressionType.Call);
        }

        [Fact]
        public void AssignmentTest()
        {
            const string fileName = "AssignmentTest";
            const string src = @"
            let a = 5
            a = 3
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (stmts, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(2, stmts.Length);
            Assert.Equal(ExpressionType.Assignment, stmts[1].Expression.Type);
        }

        [Fact]
        public void UnaryOperatorTest()
        {
            Token[] tokens = [new(TokenType.Not, 0), new(TokenType.LiteralFalse, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Unary, ast.Type);

            var unary = (UnaryExpr)ast;
            Assert.Equal(TokenType.Not, unary.Operator.Kind);
            Assert.NotNull(unary.Expression);
            Assert.Equal(TokenType.LiteralFalse, (unary.Expression as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void EqualityTest()
        {
            Token[] tokens = [new(TokenType.LiteralTrue, 0), new(TokenType.Equal, 0), new(TokenType.LiteralFalse, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "EqualityTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Equal, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(TokenType.LiteralTrue, (binary.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(binary.Right);
            Assert.Equal(TokenType.LiteralFalse, (binary.Right as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void ComparisonTest()
        {
            Token[] tokens = [new(TokenType.LiteralInt, 0, 2), new(TokenType.Greater, 0), new(TokenType.LiteralInt, 0, 1), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "ComparisonTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Greater, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(1, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void TermTest()
        {
            Token[] tokens = [new(TokenType.LiteralInt, 0, 2), new(TokenType.Minus, 0), new(TokenType.LiteralInt, 0, 3), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "TermTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Minus, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void FactorTest()
        {
            Token[] tokens = [new(TokenType.LiteralInt, 0, 6), new(TokenType.Slash, 0), new(TokenType.LiteralInt, 0, 3), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "FactorTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Binary, ast.Type);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenType.Slash, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(6, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void LogicalAndTest()
        {
            Token[] tokens = [
                new(TokenType.LiteralTrue, 0), new(TokenType.And, 0), new(TokenType.LiteralFalse, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "LogicalAndTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Logical, ast.Type);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenType.And, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.Equal(TokenType.LiteralTrue, (logicalAnd.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(logicalAnd.Right);
            Assert.Equal(TokenType.LiteralFalse, (logicalAnd.Right as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void LogicalOrTest()
        {
            Token[] tokens = [
                new(TokenType.LiteralTrue, 0), new(TokenType.Or, 0), new(TokenType.LiteralFalse, 0), new(TokenType.Semicolon, 0)];
            Parser parser = new(tokens, "LogicalOrTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionType.Logical, ast.Type);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenType.Or, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.Equal(TokenType.LiteralTrue, (logicalAnd.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(logicalAnd.Right);
            Assert.Equal(TokenType.LiteralFalse, (logicalAnd.Right as LiteralExpr).Token.Kind);
        }
    }
}
