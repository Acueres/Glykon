using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;
using Tests.Infrastructure;

namespace Tests
{
    public class ParserTests : CompilerTestBase
    {
        [Fact]
        public void BlockStatement()
        {
            const string src = @"
            let i = 6
            {
                let i = 5
            }
            ";

            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
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
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
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
            const string src = @"
            let condition = true
            while condition {
                println('ok')
            }
            ";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Equal(2, syntaxTree.Length);

            WhileStmt whileStmt = (WhileStmt)syntaxTree[1];
            Assert.NotNull(whileStmt.Body);
            Assert.NotNull(whileStmt.Condition);
        }

        [Fact]
        public void FunctionDeclaration()
        {
            const string src = @"
            def f(a: int, b: int) -> int {
                return a + b
            }
            ";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Function, syntaxTree.First().Kind);

            FunctionDeclaration function = (FunctionDeclaration)syntaxTree.First();
            Assert.Equal("f", function.Name);
            Assert.Equal("int", function.ReturnType.Name);
            Assert.Equal(2, function.Parameters.Count);
            Assert.NotNull(function.Body);
            Assert.Single(function.Body.Statements);
            Assert.Equal(StatementKind.Return, function.Body.Statements.Single().Kind);
        }

        [Fact]
        public void ConstantDeclaration()
        {
            const string src = "const pi: real = 3.14";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Constant, syntaxTree.First().Kind);
        }

        [Fact]
        public void VariableDeclaration()
        {
            const string src = "let value = 42;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotEmpty(syntaxTree);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Variable, syntaxTree.First().Kind);
            VariableDeclaration stmt = (VariableDeclaration)syntaxTree.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal("none", stmt.DeclaredType.Name);
            Assert.Equal(42, ((LiteralExpr)stmt.Expression).Value.Int);
        }

        [Fact]
        public void VariableTypeDeclaration()
        {
            const string src = "let value: int = 42;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotEmpty(syntaxTree);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Variable, syntaxTree.First().Kind);
            VariableDeclaration stmt = (VariableDeclaration)syntaxTree.First();
            Assert.Equal("value", stmt.Name);
            Assert.NotNull(stmt.Expression);
            Assert.Equal("int", stmt.DeclaredType.Name);
            Assert.Equal(42, (stmt.Expression as LiteralExpr).Value.Int);
        }

        [Fact]
        public void Call()
        {
            const string src = @"
            function('call test')
            ";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            Assert.True(syntaxTree.First() is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree.First();
            Assert.True(exprStmt.Expression is CallExpr);
        }

        [Fact]
        public void Assignment()
        {
            const string src = @"
            let a = 5
            a = 3
"; 
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Equal(2, syntaxTree.Length);

            Assert.True(syntaxTree[1] is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree[1];
            Assert.True(exprStmt.Expression is AssignmentExpr);
        }

        [Fact]
        public void UnaryOperator()
        {
            const string src = "not false;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Unary, ast.Kind);

            var unary = (UnaryExpr)ast;
            Assert.Equal(TokenKind.Not, unary.Operator.Kind);
            Assert.NotNull(unary.Operand);
            Assert.False((unary.Operand as LiteralExpr).Value.Bool);
        }

        [Fact]
        public void Equality()
        {
            const string src = "true == false;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Equal, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.True((binary.Left as LiteralExpr).Value.Bool);
            Assert.NotNull(binary.Right);
            Assert.False((binary.Right as LiteralExpr).Value.Bool);
        }

        [Fact]
        public void Comparison()
        {
            const string src = "2 > 1;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Greater, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Value.Int);
            Assert.NotNull(binary.Right);
            Assert.Equal(1, (binary.Right as LiteralExpr).Value.Int);
        }

        [Fact]
        public void Term()
        {
            const string src = "2 - 3;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Minus, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(2, (binary.Left as LiteralExpr).Value.Int);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Value.Int);
        }

        [Fact]
        public void Factor()
        {
            const string src = "6 / 3;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Binary, ast.Kind);

            var binary = (BinaryExpr)ast;
            Assert.Equal(TokenKind.Slash, binary.Operator.Kind);
            Assert.NotNull(binary.Left);
            Assert.Equal(6, (binary.Left as LiteralExpr).Value.Int);
            Assert.NotNull(binary.Right);
            Assert.Equal(3, (binary.Right as LiteralExpr).Value.Int);
        }

        [Fact]
        public void LogicalAnd()
        {
            const string src = "true and false;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Logical, ast.Kind);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenKind.And, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.True((logicalAnd.Left as LiteralExpr).Value.Bool);
            Assert.NotNull(logicalAnd.Right);
            Assert.False((logicalAnd.Right as LiteralExpr).Value.Bool);
        }

        [Fact]
        public void LogicalOr()
        {
            const string src = "true or false;";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            var exprStmt = GetStmt<ExpressionStmt>(syntaxTree.Single());
            var ast = exprStmt.Expression;
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.NotNull(ast);
            Assert.Equal(ExpressionKind.Logical, ast.Kind);

            var logicalAnd = (LogicalExpr)ast;
            Assert.Equal(TokenKind.Or, logicalAnd.Operator.Kind);
            Assert.NotNull(logicalAnd.Left);
            Assert.True((logicalAnd.Left as LiteralExpr).Value.Bool);
            Assert.NotNull(logicalAnd.Right);
            Assert.False((logicalAnd.Right as LiteralExpr).Value.Bool);
        }
    }
}
