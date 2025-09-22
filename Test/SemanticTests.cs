using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Tests;

public class SemanticTests
{
    // Helpers
    private static (SyntaxTree syntaxTree, List<IGlykonError> parseErr) Parse(string src, string file)
    {
        var (tokens, _) = new Lexer(src, file).Execute();
        return new Parser(tokens, file).Execute();
    }

    private static List<IGlykonError> Check(string src, string file)
    {
        var (syntaxTree, parseErr) = Parse(src, file);
        SemanticBinder binder = new(syntaxTree, new(), file);
        binder.Bind();

        Assert.Empty(parseErr);

        return binder.GetErrors();
    }

    [Fact]
    public void VariableTypeInference()
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
        Assert.Empty(parseErrors);

        IdentifierInterner interner = new();

        var semanticAnalyzer = new SemanticAnalyzer(syntaxTree, interner, fileName);
        var (boundTree, _, semanticErrors) = semanticAnalyzer.Analyze();

        Assert.Empty(semanticErrors);
        Assert.NotEmpty(boundTree);
        Assert.Equal(2, boundTree.Length);
        Assert.Equal(StatementKind.Variable, boundTree[1].Kind);
        var stmt = (BoundVariableDeclaration)boundTree[1];

        string name = interner[stmt.Symbol.Id];
        Assert.Equal("res", name);
        Assert.NotNull(stmt.Expression);
        Assert.Equal(TokenKind.Int, stmt.VariableType);
    }

    [Fact]
    public void VariableWrongTypeInference()
    {
        const string fileName = "VariableWrongTypeInferenceTest";
        const string src = @"
            let res = (2 + 2 * 'text')
";
        Lexer lexer = new(src, fileName);
        (var tokens, _) = lexer.Execute();
        Parser parser = new(tokens, fileName);
        var (syntaxTree, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        IdentifierInterner interner = new();

        var semanticAnalyzer = new SemanticAnalyzer(syntaxTree, interner, fileName);
        var (_, _, semanticErrors) = semanticAnalyzer.Analyze();

        Assert.Single(syntaxTree);
        Assert.Single(semanticErrors);
    }
    
    [Fact]
    public void CheckVariableInsideConstantDeclaration()
    {
        const string src = """
                                let v = 1
                                const c: int = 5 * v
                            """;
        string fileName = nameof(CheckVariableInsideConstantDeclaration);
        
        Lexer lexer = new(src, fileName);
        (var tokens, _) = lexer.Execute();
        Parser parser = new(tokens, fileName);
        var (syntaxTree, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        IdentifierInterner interner = new();

        var semanticAnalyzer = new SemanticAnalyzer(syntaxTree, interner, fileName);
        var (_, _, semanticErrors) = semanticAnalyzer.Analyze();
        Assert.Single(semanticErrors);
    }

    // Calls & overloads

    [Fact]
    public void CallWithCorrectArguments()
    {
        const string code = """
            def sum(a: int, b: int) -> int { return a + b }
            let r = sum(2, 3)
        """;
        Assert.Empty(Check(code, nameof(CallWithCorrectArguments)));
    }

    [Fact]
    public void CallWithWrongArgumentType()
    {
        const string code = """
            def sum(a: int, b: int) -> int { return a + b }
            let r = sum(2, 'str')
        """;
        Assert.Single(Check(code, nameof(CallWithWrongArgumentType)));
    }

    [Fact]
    public void OverloadResolutionSuccess()
    {
        const string code = """
            def log(msg: str) { return }
            def log(level: int, msg: str) { return }
            log('hi')          # picks 1‑arg
            log(1, 'bye')      # picks 2‑arg
        """;
        Assert.Empty(Check(code, nameof(OverloadResolutionSuccess)));
    }

    [Fact]
    public void OverloadResolutionFailure()
    {
        const string code = """
            def log(msg: str) { return }
            def log(level: int, msg: str) { return }
            log(true, 'oops')   # no matching overload
        """;
        Assert.Single(Check(code, nameof(OverloadResolutionFailure)));
    }

    [Fact]
    public void CallWithUnknownIdentifier()
    {
        const string fileName = "CallWithUnknownIdentifier";
        const string src = @"
            foo()      # unknown
        ";

        var lexer = new Lexer(src, fileName);
        (var tokens, _) = lexer.Execute();
        var parser = new Parser(tokens, fileName);
        var (syntax, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        var interner = new IdentifierInterner();
        var analyzer = new SemanticAnalyzer(syntax, interner, fileName);
        var (_, _, semanticErrors) = analyzer.Analyze();

        Assert.Single(semanticErrors);
    }

    [Fact]
    public void CallOnVariableNotCallable()
    {
        const string fileName = "CallOnVariableNotCallable";
        const string src = @"
            let x = 1
            x()        # variable, not a function
        ";

        var lexer = new Lexer(src, fileName);
        (var tokens, _) = lexer.Execute();
        var parser = new Parser(tokens, fileName);
        var (syntax, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        var interner = new IdentifierInterner();
        var analyzer = new SemanticAnalyzer(syntax, interner, fileName);
        var (_, _, semanticErrors) = analyzer.Analyze();

        Assert.Single(semanticErrors);
    }

    [Fact]
    public void CallWithParenthesizedIdentifier()
    {
        const string fileName = "CallWithParenthesizedIdentifier";
        const string src = @"
            def ping(): return
            (ping)()   # grouping around identifier is allowed
        ";

        var lexer = new Lexer(src, fileName);
        (var tokens, _) = lexer.Execute();
        var parser = new Parser(tokens, fileName);
        var (syntax, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        var interner = new IdentifierInterner();
        var analyzer = new SemanticAnalyzer(syntax, interner, fileName);
        var (_, _, semanticErrors) = analyzer.Analyze();

        Assert.Empty(semanticErrors);
    }

    [Fact]
    public void CallOverloadNoMatch()
    {
        const string fileName = "CallOverloadNoMatch";
        const string src = @"
            def log(i: int): return
            def log(i: int, j: int): return
            log(true)     # no matching overload for (bool)
        ";

        var lexer = new Lexer(src, fileName);
        (var tokens, _) = lexer.Execute();
        var parser = new Parser(tokens, fileName);
        var (syntax, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        var interner = new IdentifierInterner();
        var analyzer = new SemanticAnalyzer(syntax, interner, fileName);
        var (_, _, semanticErrors) = analyzer.Analyze();

        Assert.Single(semanticErrors);
    }

    [Fact]
    public void CallOverloadExactMatch()
    {
        const string fileName = "CallOverloadExactMatch";
        const string src = @"
            def log(i: int): return
            def log(i: int, j: int): return
            log(1)
            log(1, 2)
        ";

        var lexer = new Lexer(src, fileName);
        (var tokens, _) = lexer.Execute();
        var parser = new Parser(tokens, fileName);
        var (syntax, parseErrors) = parser.Execute();
        Assert.Empty(parseErrors);

        var interner = new IdentifierInterner();
        var analyzer = new SemanticAnalyzer(syntax, interner, fileName);
        var (_, _, semanticErrors) = analyzer.Analyze();

        Assert.Empty(semanticErrors);
    }

    // Symbol table tests

    [Fact]
    public void QualifiedNameLocalFunction()
    {
        var interner = new IdentifierInterner();
        var table = new SymbolTable(interner);

        var main = table.RegisterFunction("main", TokenKind.Int, []);
        Assert.NotNull(main);

        table.BeginScope(main!);
        var add = table.RegisterFunction("add", TokenKind.Int, [TokenKind.Int, TokenKind.Int]);
        Assert.NotNull(add);

        // Qualified name should be "main.add"
        var simple = interner[add!.Id];
        var qualified = interner[add.QualifiedId];

        Assert.Equal("add", simple);
        Assert.Equal("main.add", qualified);
    }

    [Fact]
    public void QualifiedNameDeeplyNested()
    {
        var interner = new IdentifierInterner();
        var table = new SymbolTable(interner);

        var main = table.RegisterFunction("main", TokenKind.Int, []);
        Assert.NotNull(main);

        table.BeginScope(main!);
        var inner = table.RegisterFunction("inner", TokenKind.Int, []);
        Assert.NotNull(inner);

        table.BeginScope(inner!);
        var add = table.RegisterFunction("add", TokenKind.Int, [TokenKind.Int, TokenKind.Int]);
        Assert.NotNull(add);

        var qualified = interner[add!.QualifiedId];
        Assert.Equal("main.inner.add", qualified);
    }

    [Fact]
    public void QualifiedNameTopLevelFunction()
    {
        var interner = new IdentifierInterner();
        var table = new SymbolTable(interner);

        var top = table.RegisterFunction("util", TokenKind.Int, []);
        Assert.NotNull(top);

        var simple = interner[top!.Id];
        var qualified = interner[top.QualifiedId];

        Assert.Equal("util", simple);
        Assert.Equal("util", qualified);
    }
}
