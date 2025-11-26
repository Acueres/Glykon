using System.Runtime.CompilerServices;

using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Statements;

namespace Tests.Infrastructure;

public abstract class CompilerTestBase
{
    protected LexResult Lex(string source, [CallerMemberName] string? testName = null)
    {
        var file = testName ?? GetType().Name;
        var text = new SourceText(file, source);

        var result = new Lexer(text, file).Lex();
        return result;
    }

    protected ParseResult Parse(string source, [CallerMemberName] string? testName = null)
    {
        var file = testName ?? GetType().Name;
        var text = new SourceText(file, source);

        var lexResult = new Lexer(text, file).Lex();
        var parseResult = new Parser(lexResult, file).Parse();
        
        return parseResult;
    }

    protected SemanticResult Analyze(string source, LanguageMode mode, [CallerMemberName] string? testName = null)
    {
        var file = testName ?? GetType().Name;
        var text = new SourceText(file, source);

        var lexResult = new Lexer(text, file).Lex();
        var parseResult = new Parser(lexResult, file).Parse();

        var analyzer = new SemanticAnalyzer(parseResult, mode, file);
        var semanticResult = analyzer.Analyze();

        return semanticResult;
    }
    
    protected static T GetStmt<T>(Statement s)
        => Assert.IsType<T>(s);
    
    protected static IRFunctionDeclaration GetFunction(IRStatement s)
        => Assert.IsType<IRFunctionDeclaration>(s);
    
    protected static IRVariableDeclaration GetVar(IRStatement s)
        => Assert.IsType<IRVariableDeclaration>(s);

    protected static IRLiteralExpr GetLit(IRExpression e)
        => Assert.IsType<IRLiteralExpr>(e);
    
    protected static IRConstantDeclaration GetConst(IRStatement s)
        => Assert.IsType<IRConstantDeclaration>(s);
    
    protected static IRWhileStmt GetWhileStmt(IRStatement s)
        => Assert.IsType<IRWhileStmt>(s);
    
    protected static IRBlockStmt GetBlockStmt(IRStatement s)
        => Assert.IsType<IRBlockStmt>(s);
    
    protected static IRExpressionStmt GetExprStmt(IRStatement s)
        => Assert.IsType<IRExpressionStmt>(s);
    
    protected static IRBinaryExpr GetBinary(IRExpression e)
        => Assert.IsType<IRBinaryExpr>(e);
    
    protected static IRAssignmentExpr GetAssignment(IRExpression e)
        => Assert.IsType<IRAssignmentExpr>(e);
}