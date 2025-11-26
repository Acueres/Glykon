using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;

namespace Glykon.Compiler.Semantics.Rewriting;

public abstract class IRTreeRewriter
{
    protected virtual IRStatement VisitStmt(IRStatement s) =>
        s switch
        {
            IRBlockStmt b => RewriteBlock(b),
            IRVariableDeclaration decl => RewriteVariableDeclaration(decl),
            IRConstantDeclaration c => RewriteConstantDeclaration(c),
            IRFunctionDeclaration f => RewriteFunctionDeclaration(f),
            IRIfStmt i => RewriteIf(i),
            IRWhileStmt w => RewriteWhile(w),
            IRForStmt f => RewriteFor(f),
            IRReturnStmt r => RewriteReturn(r),
            IRExpressionStmt e => RewriteExprStmt(e),
            _ => s
        };

    protected virtual IRExpression VisitExpr(IRExpression e) =>
        e switch
        {
            IRUnaryExpr u => RewriteUnary(u),
            IRBinaryExpr b => RewriteBinary(b),
            IRLogicalExpr l => RewriteLogical(l),
            IRVariableExpr v => RewriteVariable(v),
            IRAssignmentExpr a => RewriteAssignment(a),
            IRRangeExpr r => RewriteRange(r),
            IRCallExpr c => RewriteCall(c),
            IRGroupingExpr g => RewriteGrouping(g),
            IRConversionExpr cnv => RewriteConversion(cnv),
            _ => e
        };

    protected IRStatement RewriteBlock(IRBlockStmt b)
    {
        var changed = false;
        var list = new List<IRStatement>(b.Statements.Length);
        foreach (var s in b.Statements)
        {
            var ns = VisitStmt(s);
            changed |= !ReferenceEquals(ns, s);
            list.Add(ns);
        }

        return changed ? new IRBlockStmt([..list], b.Scope) : b;
    }

    protected IRStatement RewriteVariableDeclaration(IRVariableDeclaration s)
    {
        var initializer = VisitExpr(s.Initializer);
        return ReferenceEquals(initializer, s.Initializer) ? s : new IRVariableDeclaration(initializer, s.Symbol);
    }
    
    protected virtual IRStatement RewriteConstantDeclaration(IRConstantDeclaration c)
    {
        var initializer = VisitExpr(c.Initializer);
        return ReferenceEquals(initializer, c.Initializer) ? c : new IRConstantDeclaration(initializer, c.Symbol);
    }
    
    protected IRStatement RewriteFunctionDeclaration(IRFunctionDeclaration f)
    {
        var body = (IRBlockStmt)VisitStmt(f.Body);
        return ReferenceEquals(body, f.Body)
            ? f
            : new IRFunctionDeclaration(f.Signature, f.Parameters, f.ReturnType, body);
    }

    protected virtual IRStatement RewriteIf(IRIfStmt ifStmt)
    {
        var cond = VisitExpr(ifStmt.Condition);
        var thenS = VisitStmt(ifStmt.ThenStatement);
        var elseS = ifStmt.ElseStatement is null ? null : VisitStmt(ifStmt.ElseStatement);
        if (ReferenceEquals(cond, ifStmt.Condition) && ReferenceEquals(thenS, ifStmt.ThenStatement) &&
            ReferenceEquals(elseS, ifStmt.ElseStatement))
            return ifStmt;
        return new IRIfStmt(cond, thenS, elseS);
    }

    protected virtual IRStatement RewriteWhile(IRWhileStmt whileStmt)
    {
        var cond = VisitExpr(whileStmt.Condition);
        var body = VisitStmt(whileStmt.Body);
        if (ReferenceEquals(cond, whileStmt.Condition) && ReferenceEquals(body, whileStmt.Body)) return whileStmt;
        return new IRWhileStmt(cond, body);
    }

    protected virtual IRStatement RewriteFor(IRForStmt forStmt)
    {
        var iter = VisitStmt(forStmt.Iterator);
        var range = VisitExpr(forStmt.Range);
        var body = VisitStmt(forStmt.Body);
        if (ReferenceEquals(range, forStmt.Range) && ReferenceEquals(body, forStmt.Body)) return forStmt;
        return new IRForStmt((IRVariableDeclaration)iter, (IRRangeExpr)range, (IRBlockStmt)body);
    }

    protected virtual IRStatement RewriteReturn(IRReturnStmt r)
    {
        var expr = r.Value is null ? null : VisitExpr(r.Value);
        return ReferenceEquals(expr, r.Value) ? r : new IRReturnStmt(expr, r.Token);
    }

    protected virtual IRStatement RewriteExprStmt(IRExpressionStmt e)
    {
        var expr = VisitExpr(e.Expression);
        return ReferenceEquals(expr, e.Expression) ? e : new IRExpressionStmt(expr);
    }

    protected virtual IRExpression RewriteUnary(IRUnaryExpr unaryExpr)
    {
        var operand = VisitExpr(unaryExpr.Operand);
        return ReferenceEquals(operand, unaryExpr.Operand) ? unaryExpr : new IRUnaryExpr(unaryExpr.Operator, operand, unaryExpr.Type);
    }

    protected virtual IRExpression RewriteBinary(IRBinaryExpr binaryExpr)
    {
        var left = VisitExpr(binaryExpr.Left);
        var right = VisitExpr(binaryExpr.Right);
        return ReferenceEquals(left, binaryExpr.Left) && ReferenceEquals(right, binaryExpr.Right)
            ? binaryExpr
            : new IRBinaryExpr(binaryExpr.Operator, left, right, binaryExpr.Type);
    }

    protected virtual IRExpression RewriteLogical(IRLogicalExpr logicalExpr)
    {
        var left = VisitExpr(logicalExpr.Left);
        var right = VisitExpr(logicalExpr.Right);
        return ReferenceEquals(left, logicalExpr.Left) && ReferenceEquals(right, logicalExpr.Right)
            ? logicalExpr
            : new IRBinaryExpr(logicalExpr.Operator, left, right, logicalExpr.Type);
    }
    
    protected virtual IRExpression RewriteVariable(IRVariableExpr variableExpr)
    {
        return variableExpr;
    }
    
    protected virtual IRExpression RewriteAssignment(IRAssignmentExpr a)
    {
        var value = VisitExpr(a.Value);
        return ReferenceEquals(value, a.Value)
            ? a
            : new IRAssignmentExpr(value, a.Symbol);
    }
    
    protected virtual IRExpression RewriteRange(IRRangeExpr r)
    {
        var start = VisitExpr(r.Start);
        var end = VisitExpr(r.End);
        var step = VisitExpr(r.Step);
        
        return ReferenceEquals(start, r.Start) && ReferenceEquals(end, r.End) && ReferenceEquals(step, r.Step)
            ? r
            : new IRRangeExpr(start, end, step, r.IsInclusive);
    }

    protected virtual IRExpression RewriteCall(IRCallExpr c)
    {
        var changed = false;
        var parameters = new IRExpression[c.Parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            var a = VisitExpr(c.Parameters[i]);
            changed |= !ReferenceEquals(a, c.Parameters[i]);
            parameters[i] = a;
        }

        return changed ? new IRCallExpr(c.Function, parameters) : c;
    }
    
    protected virtual IRExpression RewriteGrouping(IRGroupingExpr g)
    {
        var expr = VisitExpr(g.Expression);
        return ReferenceEquals(expr, g.Expression)
            ? g
            : new IRGroupingExpr(expr);
    }
    
    protected virtual IRExpression RewriteConversion(IRConversionExpr cnv)
    {
        var expr = VisitExpr(cnv.Expression);
        return ReferenceEquals(expr, cnv.Expression)
            ? cnv
            : new IRConversionExpr(expr, cnv.Type);
    }
}