using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Tests
{
    public class ParserTests
    {
        [Fact]
        public void BlockStatement()
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
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.NotEmpty(syntaxTree);
            Assert.Equal(2, syntaxTree.Length);
            Assert.Equal(StatementKind.Block, syntaxTree[1].Kind);
            BlockStmt stmt = (BlockStmt)syntaxTree[1];
            Assert.Single(stmt.Statements);
        }

        [Fact]
        public void IfStatement()
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
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(3, syntaxTree.Length);

            IfStmt ifStmt = (IfStmt)syntaxTree[2];
            Assert.NotNull(ifStmt.ElseStatement);

            IfStmt elifStmt = (IfStmt)ifStmt.ElseStatement;
            Assert.NotNull(elifStmt.ElseStatement);

            IfStmt elseifStmt = (IfStmt)elifStmt.ElseStatement;
            Assert.NotNull(elseifStmt.ElseStatement);
        }

        [Fact]
        public void WhileStatement()
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
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(2, syntaxTree.Length);

            WhileStmt whileStmt = (WhileStmt)syntaxTree[1];
            Assert.NotNull(whileStmt.Statement);
            Assert.NotNull(whileStmt.Condition);
        }

        [Fact]
        public void FunctionDeclaration()
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
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Function, syntaxTree.First().Kind);

            FunctionDeclaration function = (FunctionDeclaration)syntaxTree.First();
            Assert.Equal("f", function.Name);
            Assert.Equal(TokenKind.Int, function.ReturnType);
            Assert.Equal(2, function.Parameters.Count);
            Assert.NotNull(function.Body);
            Assert.Single(function.Body.Statements);
            Assert.Equal(StatementKind.Return, function.Body.Statements.Single().Kind);
        }

        [Fact]
        public void ConstantDeclaration()
        {
            const string fileName = "ConstantDeclarationTest";

            Token[] tokens = [new(TokenKind.Const, 0), new(TokenKind.Identifier, 0, "pi"),
            new(TokenKind.Colon, 0), new(TokenKind.Real, 0),
            new(TokenKind.Assignment, 0), new(TokenKind.LiteralReal, 0, 3.14), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, fileName);
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Constant, syntaxTree.First().Kind);

            SemanticBinder binder = new(syntaxTree, new(), fileName);
            var (_, st) = binder.Bind();

            var symbol = st.GetSymbol("pi");
            Assert.NotNull(symbol);
            Assert.True(symbol is ConstantSymbol);

            var constant = (ConstantSymbol)symbol;
            Assert.Equal(3.14, constant.Value.RealValue);
        }

        [Fact]
        public void VariableDeclaration()
        {
            Token[] tokens = [new(TokenKind.Let, 0), new(TokenKind.Identifier, 0, "value"), new(TokenKind.Assignment, 0), new(TokenKind.LiteralInt, 0, 42), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "VariableDeclarationTest");
            var (syntaxTree, _) = parser.Execute();

            Assert.NotEmpty(syntaxTree);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Variable, syntaxTree.First().Kind);
            VariableDeclaration stmt = (VariableDeclaration)syntaxTree.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenKind.None, stmt.DeclaredType); // Parser cannot infer types
            Assert.Equal(42, ((LiteralExpr)stmt.Expression).Token.IntValue);
        }

        [Fact]
        public void VariableTypeDeclaration()
        {
            Token[] tokens = [new(TokenKind.Let, 0), new(TokenKind.Identifier, 0, "value"),
                new(TokenKind.Colon, 0), new(TokenKind.Int, 0),
                new(TokenKind.Assignment, 0), new(TokenKind.LiteralInt, 0, 42), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "VariableTypeDeclarationTest");
            var (syntaxTree, _) = parser.Execute();

            Assert.NotEmpty(syntaxTree);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Variable, syntaxTree.First().Kind);
            VariableDeclaration stmt = (VariableDeclaration)syntaxTree.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal(TokenKind.Int, stmt.DeclaredType);
            Assert.Equal(42, (stmt.Expression as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void Call()
        {
            const string fileName = "CallTest";
            const string src = @"
            function('call test')
            ";

            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            Assert.True(syntaxTree.First() is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree.First();
            Assert.True(exprStmt.Expression is CallExpr);
        }

        [Fact]
        public void Assignment()
        {
            const string fileName = "AssignmentTest";
            const string src = @"
            let a = 5
            a = 3
";
            Lexer lexer = new(src, fileName);
            (var tokens, _) = lexer.Execute();
            Parser parser = new(tokens, fileName);
            var (syntaxTree, errors) = parser.Execute();

            Assert.Empty(errors);
            Assert.Equal(2, syntaxTree.Length);

            Assert.True(syntaxTree[1] is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree[1];
            Assert.True(exprStmt.Expression is AssignmentExpr);
        }

        [Fact]
        public void UnaryOperator()
        {
            Token[] tokens = [new(TokenKind.Not, 0), new(TokenKind.LiteralFalse, 0), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "UnaryTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Unary, ast.Kind);

            var unary = (UnaryExpr)ast;
            Assert.Equal(TokenKind.Not, unary.Operator.Kind);
            Assert.NotNull(unary.Operand);
            Assert.Equal(TokenKind.LiteralFalse, (unary.Operand as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void Equality()
        {
            Token[] tokens = [new(TokenKind.LiteralTrue, 0), new(TokenKind.Equal, 0), new(TokenKind.LiteralFalse, 0), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "EqualityTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Equal, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(TokenKind.LiteralTrue, (binary.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(binary.Right);
            Assert.Equal(TokenKind.LiteralFalse, (binary.Right as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void Comparison()
        {
            Token[] tokens = [new(TokenKind.LiteralInt, 0, 2), new(TokenKind.Greater, 0), new(TokenKind.LiteralInt, 0, 1), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "ComparisonTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Greater, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(1, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void Term()
        {
            Token[] tokens = [new(TokenKind.LiteralInt, 0, 2), new(TokenKind.Minus, 0), new(TokenKind.LiteralInt, 0, 3), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "TermTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Minus, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void Factor()
        {
            Token[] tokens = [new(TokenKind.LiteralInt, 0, 6), new(TokenKind.Slash, 0), new(TokenKind.LiteralInt, 0, 3), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "FactorTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Slash, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(6, (binary.Left as LiteralExpr).Token.IntValue);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Token.IntValue);
        }

        [Fact]
        public void LogicalAnd()
        {
            Token[] tokens = [
                new(TokenKind.LiteralTrue, 0), new(TokenKind.And, 0), new(TokenKind.LiteralFalse, 0), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "LogicalAndTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Logical, ast.Kind);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenKind.And, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.Equal(TokenKind.LiteralTrue, (logicalAnd.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(logicalAnd.Right);
            Assert.Equal(TokenKind.LiteralFalse, (logicalAnd.Right as LiteralExpr).Token.Kind);
        }

        [Fact]
        public void LogicalOr()
        {
            Token[] tokens = [
                new(TokenKind.LiteralTrue, 0), new(TokenKind.Or, 0), new(TokenKind.LiteralFalse, 0), new(TokenKind.Semicolon, 0)];
            Parser parser = new(tokens, "LogicalOrTest");
            var ast = parser.ParseExpression();

            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Logical, ast.Kind);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenKind.Or, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.Equal(TokenKind.LiteralTrue, (logicalAnd.Left as LiteralExpr).Token.Kind);
            Assert.NotNull(logicalAnd.Right);
            Assert.Equal(TokenKind.LiteralFalse, (logicalAnd.Right as LiteralExpr).Token.Kind);
        }
    }
}
