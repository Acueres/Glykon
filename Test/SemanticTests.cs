using TythonCompiler.Parsing;
using TythonCompiler.SemanticAnalysis;
using TythonCompiler.Syntax.Statements;
using TythonCompiler.Tokenization;

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
        var (stmts, symbolTable, parseErrors) = parser.Execute();

        Assert.Empty(parseErrors);

        var semanticAnalyzer = new SemanticAnalyzer(stmts, symbolTable, fileName);
        var semanticErrors = semanticAnalyzer.Execute();

        Assert.Empty(semanticErrors);
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
        var (stmts, symbolTable, parseErrors) = parser.Execute();

        Assert.Empty(parseErrors);

        var semanticAnalyzer = new SemanticAnalyzer(stmts, symbolTable, fileName);
        var semanticErrors = semanticAnalyzer.Execute();

        Assert.Single(stmts);
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
        var (stmts, symbolTable, parserErrors) = parser.Execute();

        Assert.Empty(parserErrors);

        SemanticAnalyzer analyzer = new(stmts, symbolTable, fileName);
        var errors = analyzer.Execute();

        Assert.Equal(2, errors.Count);
        Assert.NotEmpty(stmts);
        Assert.Equal(3, stmts.Length);
        Assert.Equal(StatementType.Jump, stmts[1].Type);
    }
}
