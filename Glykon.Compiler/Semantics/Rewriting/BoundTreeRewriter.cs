using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;

namespace Glykon.Compiler.Semantics.Rewriting;

public abstract class BoundTreeRewriter
{
    protected virtual BoundStatement VisitStmt(BoundStatement s) =>
        s switch
        {
            BoundBlockStmt b => RewriteBlock(b),
            BoundVariableDeclaration decl => RewriteVariableDeclaration(decl),
            BoundIfStmt i => RewriteIf(i),
            BoundWhileStmt w => RewriteWhile(w),
            BoundReturnStmt r => RewriteReturn(r),
            BoundExpressionStmt e => RewriteExprStmt(e),
            _ => s
        };

    protected virtual BoundExpression VisitExpr(BoundExpression e) =>
        e switch
        {
            BoundUnaryExpr u => RewriteUnary(u),
            BoundBinaryExpr b => RewriteBinary(b),
            BoundLogicalExpr l => RewriteLogical(l),
            BoundCallExpr c => RewriteCall(c),
            _ => e
        };

    protected BoundStatement RewriteBlock(BoundBlockStmt b)
    {
        var changed = false;
        var list = new List<BoundStatement>(b.Statements.Length);
        foreach (var s in b.Statements)
        {
            var ns = VisitStmt(s);
            changed |= !ReferenceEquals(ns, s);
            list.Add(ns);
        }

        return changed ? new BoundBlockStmt([..list], b.Scope) : b;
    }

    protected BoundStatement RewriteVariableDeclaration(BoundVariableDeclaration s)
    {
        var expr = VisitExpr(s.Expression);
        return ReferenceEquals(expr, s.Expression) ? s : new BoundVariableDeclaration(expr, s.Symbol, s.VariableType);
    }

    protected virtual BoundStatement RewriteIf(BoundIfStmt ifStmt)
    {
        var cond = VisitExpr(ifStmt.Condition);
        var thenS = VisitStmt(ifStmt.ThenStatement);
        var elseS = ifStmt.ElseStatement is null ? null : VisitStmt(ifStmt.ElseStatement);
        if (ReferenceEquals(cond, ifStmt.Condition) && ReferenceEquals(thenS, ifStmt.ThenStatement) &&
            ReferenceEquals(elseS, ifStmt.ElseStatement))
            return ifStmt;
        return new BoundIfStmt(cond, thenS, elseS);
    }

    protected virtual BoundStatement RewriteWhile(BoundWhileStmt whileStmt)
    {
        var cond = VisitExpr(whileStmt.Condition);
        var body = VisitStmt(whileStmt.Body);
        if (ReferenceEquals(cond, whileStmt.Condition) && ReferenceEquals(body, whileStmt.Body)) return whileStmt;
        return new BoundWhileStmt(cond, body);
    }

    protected virtual BoundStatement RewriteReturn(BoundReturnStmt r)
    {
        var expr = r.Expression is null ? null : VisitExpr(r.Expression);
        return ReferenceEquals(expr, r.Expression) ? r : new BoundReturnStmt(expr, r.Token);
    }

    protected virtual BoundStatement RewriteExprStmt(BoundExpressionStmt e)
    {
        var expr = VisitExpr(e.Expression);
        return ReferenceEquals(expr, e.Expression) ? e : new BoundExpressionStmt(expr);
    }

    protected virtual BoundExpression RewriteUnary(BoundUnaryExpr unaryExpr)
    {
        var opnd = VisitExpr(unaryExpr.Operand);
        return ReferenceEquals(opnd, unaryExpr.Operand) ? unaryExpr : new BoundUnaryExpr(unaryExpr.Operator, opnd);
    }

    protected virtual BoundExpression RewriteBinary(BoundBinaryExpr binaryExpr)
    {
        var left = VisitExpr(binaryExpr.Left);
        var right = VisitExpr(binaryExpr.Right);
        return ReferenceEquals(left, binaryExpr.Left) && ReferenceEquals(right, binaryExpr.Right)
            ? binaryExpr
            : new BoundBinaryExpr(binaryExpr.Operator, left, right);
    }

    protected virtual BoundExpression RewriteLogical(BoundLogicalExpr logicalExpr)
    {
        var left = VisitExpr(logicalExpr.Left);
        var right = VisitExpr(logicalExpr.Right);
        return ReferenceEquals(left, logicalExpr.Left) && ReferenceEquals(right, logicalExpr.Right)
            ? logicalExpr
            : new BoundBinaryExpr(logicalExpr.Operator, left, right);
    }

    protected virtual BoundExpression RewriteCall(BoundCallExpr c)
    {
        var changed = false;
        var args = new BoundExpression[c.Args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            var a = VisitExpr(c.Args[i]);
            changed |= !ReferenceEquals(a, c.Args[i]);
            args[i] = a;
        }

        return changed ? new BoundCallExpr(c.Function, args) : c;
    }
}