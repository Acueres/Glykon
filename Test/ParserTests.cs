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
            const string src = """

                                           let i = 6
                                           {
                                               let i = 5
                                           }
                                           
                               """;

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
            const string src = """

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
                                           
                               """;
            
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
            const string src = """

                                           let condition = true
                                           while condition {
                                               println('ok')
                                           }
                                           
                               """;
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Equal(2, syntaxTree.Length);

            var whileStmt = (WhileStmt)syntaxTree[1];
            Assert.NotNull(whileStmt.Body);
            Assert.NotNull(whileStmt.Condition);
        }
        
        [Fact]
        public void ForStatement()
        {
            const string src = """
                               for i in 0..=10 {
                                    println(i)
                               }
                               """;
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            var forStmt = (ForStmt)syntaxTree.Single();
            Assert.NotNull(forStmt.Iterator);
            Assert.Equal("i", forStmt.Iterator.Name);
            Assert.NotNull(forStmt.Range);
            Assert.True(forStmt.Range.IsInclusive);
            Assert.NotNull(forStmt.Body);
        }
        
        [Fact]
        public void ClassDeclaration()
        {
            const string src = """
                                    class Test {
                                           uninitialized: int
                                           initialized: int = 1
                                           
                                           const pi: real = 3.14
                                           
                                           def add(self, a: int, b: int) -> int {
                                               return a + b
                                           }
                                           
                                           def static_add(a: int, b: int) -> int {
                                                return a + b
                                            }
                                    }
                               """;
            
            var (syntaxTree, _, lexErrors, parseErrors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(parseErrors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Type, syntaxTree.First().Kind);

            var classDecl = (TypeDeclaration)syntaxTree.First();
            Assert.Equal("Test", classDecl.Name);
            
            var methods = classDecl.Methods;

            Assert.Equal(2, methods.Length);
            Assert.False(methods[0].IsStatic);
            Assert.True(methods[1].IsStatic);
            
            var fields = classDecl.Fields;
            Assert.Equal(2, fields.Length);
            
            var constants = classDecl.Constants;
            Assert.Single(constants);
        }

        [Fact]
        public void FunctionDeclaration()
        {
            const string src = """

                                           def f(a: int, b: int) -> int {
                                               return a + b
                                           }
                                           
                               """;
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);
            Assert.Equal(StatementKind.Function, syntaxTree.First().Kind);

            FunctionDeclaration function = (FunctionDeclaration)syntaxTree.First();
            Assert.Equal("f", function.Name);

            var name = (NameExpr)function.ReturnType.Expression;
            Assert.Equal("int", name.Name);
            Assert.Equal(2, function.Parameters.Length);
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
            Assert.NotNull(stmt.Initializer);
            
            var name = (NameExpr)stmt.DeclaredType.Expression;
            Assert.Equal("none", name.Name);
            Assert.Equal(42, ((LiteralExpr)stmt.Initializer).Value.Int);
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
            Assert.NotNull(stmt.Initializer);
            
            var name = (NameExpr)stmt.DeclaredType.Expression;
            Assert.Equal("int", name.Name);
            Assert.Equal(42, (stmt.Initializer as LiteralExpr).Value.Int);
        }

        [Fact]
        public void Call()
        {
            const string src = """

                                           function('call test')
                                           
                               """;
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            Assert.True(syntaxTree.First() is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree.First();
            Assert.True(exprStmt.Expression is CallExpr);
        }

        [Fact]
        public void Conversion()
        {
            const string src = """

                                           42 as str
                               """; 
            var (syntaxTree, _, lexErrors, errors) = Parse(src);
            
            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            Assert.True(syntaxTree[0] is ExpressionStmt);
            ExpressionStmt exprStmt = (ExpressionStmt)syntaxTree[0];
            Assert.True(exprStmt.Expression is ConversionExpr);
            Assert.True(exprStmt.Expression is ConversionExpr);
            ConversionExpr conversionExpr = (ConversionExpr)exprStmt.Expression;
            Assert.Equal(ExpressionKind.Literal, conversionExpr.Expression.Kind);

            var name = (NameExpr)conversionExpr.TargetType.Expression;
            Assert.Equal("str", name.Name);
        }

        [Fact]
        public void Assignment()
        {
            const string src = """
                                            let a = 5
                                            a = 3
                               """; 
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
            Assert.True(((LiteralExpr)logicalAnd.Left).Value.Bool);
            Assert.NotNull(logicalAnd.Right);
            Assert.False(((LiteralExpr)logicalAnd.Right).Value.Bool);
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
            Assert.True(((LiteralExpr)logicalAnd.Left).Value.Bool);
            Assert.NotNull(logicalAnd.Right);
            Assert.False(((LiteralExpr)logicalAnd.Right).Value.Bool);
        }

        [Fact]
        public void InitObject()
        {
            const string src = "let a = new A { field1: 1, field2: 2 }";
            
            var (syntaxTree, _, lexErrors, errors) = Parse(src);

            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            
            var variableDeclaration = GetStmt<VariableDeclaration>(syntaxTree.Single());
            Assert.Equal(ExpressionKind.Initializer, variableDeclaration.Initializer.Kind);

            var initObj = (InitializerExpr)variableDeclaration.Initializer;
            Assert.Equal(ExpressionKind.Name, initObj.TypeName.Expression.Kind);
            
            var name = (NameExpr)initObj.TypeName.Expression;
            Assert.Equal("A", name.Name);
            
            Assert.Equal(2, initObj.Initializers.Length);
            var first = initObj.Initializers[0];
            var second = initObj.Initializers[1];
            
            Assert.Equal("field1", first.Name);
            Assert.Equal("field2", second.Name);
            
            Assert.Equal(ExpressionKind.Literal, first.Value.Kind);
            Assert.Equal(ExpressionKind.Literal, second.Value.Kind);
        }
        
        [Fact]
        public void QualifiedTypeAnnotationsInFunctionDeclaration()
        {
            const string src = """
                               def f(v: Outer.Inner, w: A.B.C) -> Outer.Inner {
                                   return v
                               }
                               """;

            var (syntaxTree, _, lexErrors, errors) = Parse(src);

            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            var function = (FunctionDeclaration)syntaxTree.Single();
            Assert.Equal(2, function.Parameters.Length);

            AssertTypePath(function.Parameters[0].Type.Expression, "Outer", "Inner");
            AssertTypePath(function.Parameters[1].Type.Expression, "A", "B", "C");
            AssertTypePath(function.ReturnType.Expression, "Outer", "Inner");
        }

        [Fact]
        public void QualifiedTypeInConversionExpression()
        {
            const string src = """
                               value as Outer.Inner
                               """;

            var (syntaxTree, _, lexErrors, errors) = Parse(src);

            Assert.Empty(lexErrors);
            Assert.Empty(errors);
            Assert.Single(syntaxTree);

            var exprStmt = Assert.IsType<ExpressionStmt>(syntaxTree.Single());
            var conv = Assert.IsType<ConversionExpr>(exprStmt.Expression);

            AssertTypePath(conv.TargetType.Expression, "Outer", "Inner");
        }

        private static void AssertTypePath(Expression expr, params string[] expected)
        {
            var actual = FlattenTypePath(expr).ToArray();
            Assert.Equal(expected, actual);
        }

        private static List<string> FlattenTypePath(Expression expr)
        {
            var parts = new List<string>();

            while (expr is MemberAccessExpr ma)
            {
                parts.Add(GetMemberName(ma));
                expr = GetReceiver(ma);
            }

            var root = Assert.IsType<NameExpr>(expr);
            parts.Add(root.Name);

            parts.Reverse();
            return parts;
        }

        private static Expression GetReceiver(MemberAccessExpr ma)
        {
            var t = ma.GetType();
            var prop =
                t.GetProperty("Receiver") ??
                t.GetProperty("Expression") ??
                t.GetProperty("Target") ??
                t.GetProperties().FirstOrDefault(p => typeof(Expression).IsAssignableFrom(p.PropertyType));

            Assert.NotNull(prop);
            return (Expression)prop!.GetValue(ma)!;
        }

        private static string GetMemberName(MemberAccessExpr ma)
        {
            var t = ma.GetType();
            var prop =
                t.GetProperty("MemberName") ??
                t.GetProperty("Name") ??
                t.GetProperty("Member") ??
                t.GetProperties().FirstOrDefault(p => p.PropertyType == typeof(string));

            Assert.NotNull(prop);
            return (string)prop!.GetValue(ma)!;
        }
    }
}
