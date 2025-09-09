using Glykon.Compiler.Semantics;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Tests;

public class SemanticTests
{
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
        var (syntaxTree, parseErrors) = parser.Execute();

        SemanticBinder binder = new(syntaxTree, new());
        var symbolTable = binder.Bind();

        Assert.Empty(parseErrors);

        var semanticAnalyzer = new SemanticAnalyzer(syntaxTree, symbolTable, fileName);
        var semanticErrors = semanticAnalyzer.Execute();

        Assert.Empty(semanticErrors);
        Assert.NotEmpty(syntaxTree);
        Assert.Equal(2, syntaxTree.Length);
        Assert.Equal(StatementType.Variable, syntaxTree[1].Type);
        VariableStmt stmt = (VariableStmt)syntaxTree[1];
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
        var (syntaxTree, parseErrors) = parser.Execute();

        SemanticBinder binder = new(syntaxTree, new());
        var symbolTable = binder.Bind();

        Assert.Empty(parseErrors);

        var semanticAnalyzer = new SemanticAnalyzer(syntaxTree, symbolTable, fileName);
        var semanticErrors = semanticAnalyzer.Execute();

        Assert.Single(syntaxTree);
        Assert.Single(semanticErrors);
    }

    [Fact]
    public void JumpStatementsTest()
    {
        const string fileName = "JumpStatementsTest";
        const string src = @"
            while true {
                break
            }

            continue

            if true {
                break
            }
            ";
        Lexer lexer = new(src, fileName);
        (var tokens, _) = lexer.Execute();
        Parser parser = new(tokens, fileName);
        var (syntaxTree, parserErrors) = parser.Execute();

        SemanticBinder binder = new(syntaxTree, new());
        var symbolTable = binder.Bind();

        Assert.Empty(parserErrors);

        SemanticAnalyzer analyzer = new(syntaxTree, symbolTable, fileName);
        var errors = analyzer.Execute();

        Assert.Equal(2, errors.Count);
        Assert.NotEmpty(syntaxTree);
        Assert.Equal(3, syntaxTree.Length);
        Assert.Equal(StatementType.Jump, syntaxTree[1].Type);
    }
}
