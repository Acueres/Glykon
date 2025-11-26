using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Tests.Infrastructure;

namespace Tests;

public class LoweringTests : CompilerTestBase
{
    [Fact]
    public void TestForStmtInclusiveNoStep()
    {
        const string src = @"
        for i in 0..=10 {
            println(i)
        }
";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);
        
        var f = GetFunction(semanticResult.Ir.Single());
        
        var block = GetBlockStmt(f.Body.Statements.Single());
        Assert.Equal(2, block.Statements.Length);

        var stmts = block.Statements;

        Assert.IsType<IRVariableDeclaration>(stmts[0]);

        var whileStmt = GetWhileStmt(stmts[1]);
        var body = GetBlockStmt(whileStmt.Body);
        Assert.Equal(2, body.Statements.Length);

        var condition = GetBinary(whileStmt.Condition);
        Assert.Equal(BinaryOp.LessOrEqual, condition.Operator);

        var exprStmt = GetExprStmt(body.Statements[1]);
        var assignment = GetAssignment(exprStmt.Expression);
        var increment = GetBinary(assignment.Value);
        Assert.Equal(BinaryOp.Add, increment.Operator);
    }
    
    [Fact]
    public void TestForStmtDescendingExclusiveWithStep()
    {
        const string src = @"
        for i in 10..0 by -1 {
            println(i)
        }
";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        
        var block = GetBlockStmt(f.Body.Statements.Single());
        Assert.Equal(2, block.Statements.Length);

        var stmts = block.Statements;

        Assert.IsType<IRVariableDeclaration>(stmts[0]);

        var whileStmt = GetWhileStmt(stmts[1]);
        var body = GetBlockStmt(whileStmt.Body);
        Assert.Equal(2, body.Statements.Length);

        // i > end
        var condition = GetBinary(whileStmt.Condition);
        Assert.Equal(BinaryOp.Greater, condition.Operator);

        var exprStmt = GetExprStmt(body.Statements[1]);
        var assignment = GetAssignment(exprStmt.Expression);

        var increment = GetBinary(assignment.Value);
        Assert.Equal(BinaryOp.Add, increment.Operator);

        var stepLiteral = Assert.IsType<IRLiteralExpr>(increment.Right);
        Assert.Equal(-1, stepLiteral.Value.Int);
    }
    
    [Fact]
    public void TestForStmtDescendingInclusiveWithStep()
    {
        const string src = @"
        for i in 10..=0 by -2 {
            println(i)
        }
";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        
        var block = GetBlockStmt(f.Body.Statements.Single());
        Assert.Equal(2, block.Statements.Length);

        var stmts = block.Statements;

        Assert.IsType<IRVariableDeclaration>(stmts[0]);

        var whileStmt = GetWhileStmt(stmts[1]);
        var body = GetBlockStmt(whileStmt.Body);
        Assert.Equal(2, body.Statements.Length);

        // i >= end
        var condition = GetBinary(whileStmt.Condition);
        Assert.Equal(BinaryOp.GreaterOrEqual, condition.Operator);

        var exprStmt = GetExprStmt(body.Statements[1]);
        var assignment = GetAssignment(exprStmt.Expression);

        var increment = GetBinary(assignment.Value);
        Assert.Equal(BinaryOp.Add, increment.Operator);

        var stepLiteral = Assert.IsType<IRLiteralExpr>(increment.Right);
        Assert.Equal(-2, stepLiteral.Value.Int);
    }
    
    [Fact]
    public void TestForStmtWithVariableBounds()
    {
        const string src = @"
        let start = 0
        let end = 10
        for i in start..end {
            println(i)
        }
";
        var semanticResult = Analyze(src, LanguageMode.Script);

        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        
        // start decl, end decl, lowered while inside a block
        Assert.Equal(3, f.Body.Statements.Length);
        
        var stmts = f.Body.Statements;

        Assert.IsType<IRVariableDeclaration>(stmts[0]);
        Assert.IsType<IRVariableDeclaration>(stmts[1]);
        
        var block = GetBlockStmt(f.Body.Statements[2]);
        Assert.IsType<IRVariableDeclaration>(block.Statements[0]);
        
        var whileStmt = GetWhileStmt(block.Statements[1]);
        var body = GetBlockStmt(whileStmt.Body);
        Assert.Equal(2, body.Statements.Length);

        var condition = GetBinary(whileStmt.Condition);
        Assert.True(
            condition.Operator is BinaryOp.Less or BinaryOp.LessOrEqual);
    }
    
    [Fact]
    public void TestForStmtStepZeroIsEmptyLoop()
    {
        const string src = @"
        for i in 0..10 by 0 {
            println(i)
        }
";
        var semanticResult = Analyze(src, LanguageMode.Script);

        // Step 0 is allowed; no semantic errors.
        Assert.Empty(semanticResult.AllErrors);

        var f = GetFunction(semanticResult.Ir.Single());
        
        var block = GetBlockStmt(f.Body.Statements.Single());
        Assert.Equal(2, block.Statements.Length);

        var stmts = block.Statements;

        Assert.IsType<IRVariableDeclaration>(stmts[0]);

        var whileStmt = GetWhileStmt(stmts[1]);
        
        var literal = Assert.IsType<IRLiteralExpr>(whileStmt.Condition);
        Assert.False(literal.Value.Bool);
    }
}