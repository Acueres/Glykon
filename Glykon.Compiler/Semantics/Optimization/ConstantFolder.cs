using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Rewriting;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Optimization;

public class ConstantFolder: BoundTreeRewriter
{
    public BoundTree Fold(BoundTree boundTree)
    {
        List<BoundStatement> rewritten = new(boundTree.Length);
        rewritten.AddRange(boundTree.Select(VisitStmt));
        return new BoundTree([..rewritten], boundTree.FileName);
    }

    public BoundExpression FoldExpression(BoundExpression expr) => VisitExpr(expr);
    
    protected override BoundExpression RewriteUnary(BoundUnaryExpr unaryExpr)
    {
        var opnd = VisitExpr(unaryExpr.Operand);
        if (!ReferenceEquals(opnd, unaryExpr.Operand)) unaryExpr = new BoundUnaryExpr(unaryExpr.Operator, opnd);
        if (opnd is not BoundLiteralExpr lit) return unaryExpr;
        if (TryFoldUnary(unaryExpr.Operator.Kind, lit, out var folded)) return folded;

        return unaryExpr;
    }
    
    protected override BoundExpression RewriteBinary(BoundBinaryExpr binaryExpr)
    {
        var left = VisitExpr(binaryExpr.Left);
        var right = VisitExpr(binaryExpr.Right);
        if (!ReferenceEquals(left, binaryExpr.Left) || !ReferenceEquals(right, binaryExpr.Right))
            binaryExpr = new BoundBinaryExpr(binaryExpr.Operator, left, right);
        
        if (left is not BoundLiteralExpr l1 || right is not BoundLiteralExpr l2) return binaryExpr;
        if (TryFoldBinary(binaryExpr.Operator.Kind, l1, l2, out var folded)) return folded;

        return binaryExpr;
    }
    
    protected override BoundExpression RewriteLogical(BoundLogicalExpr logicalExpr)
    {
        var left = VisitExpr(logicalExpr.Left);
        var right = VisitExpr(logicalExpr.Right);
        if (!ReferenceEquals(left, logicalExpr.Left) || !ReferenceEquals(right, logicalExpr.Right))
            logicalExpr = new BoundLogicalExpr(logicalExpr.Operator, left, right);

        switch (logicalExpr.Operator.Kind)
        {
            // Short-circuit boolean ops
            case TokenKind.And when left is BoundLiteralExpr { Value.Kind: ConstantKind.Bool } l:
                return l.Value.Bool
                    ? right
                    : new BoundLiteralExpr(l.Value);
            case TokenKind.Or when left is BoundLiteralExpr { Value.Kind: ConstantKind.Bool } l:
                return l.Value.Bool
                    ? new BoundLiteralExpr(l.Value)
                    : right;
        }
        
        if (left is not BoundLiteralExpr l1 || right is not BoundLiteralExpr l2) return logicalExpr;
        if (TryFoldLogical(logicalExpr.Operator.Kind, l1, l2, out var folded)) return folded;
        
        return logicalExpr;
    }
    
    protected override BoundStatement RewriteIf(BoundIfStmt ifStmt)
    {
        var cond = VisitExpr(ifStmt.Condition);
        var thenS = VisitStmt(ifStmt.ThenStatement);
        var elseS = ifStmt.ElseStatement is null ? null : VisitStmt(ifStmt.ElseStatement);
        if (cond is BoundLiteralExpr { Value.Kind: ConstantKind.Bool } literal)
        {
            return literal.Value.Bool ? thenS : elseS ?? new BoundBlockStmt([], new Scope());
        }

        if (ReferenceEquals(cond, ifStmt.Condition) && ReferenceEquals(thenS, ifStmt.ThenStatement) &&
            ReferenceEquals(elseS, ifStmt.ElseStatement))
            return ifStmt;
        
        return new BoundIfStmt(cond, thenS, elseS);
    }

    protected override BoundStatement RewriteWhile(BoundWhileStmt whileStmt)
    {
        var cond = VisitExpr(whileStmt.Condition);
        var body = VisitStmt(whileStmt.Body);
        if (cond is BoundLiteralExpr { Value.Kind: ConstantKind.Bool, Value.Bool: false })
        {
            return new BoundBlockStmt([], new Scope());
        }

        if (ReferenceEquals(cond, whileStmt.Condition) && ReferenceEquals(body, whileStmt.Body)) return whileStmt;
        return new BoundWhileStmt(cond, body);
    }

    // Literal evaluation helpers
    private static bool TryFoldUnary(TokenKind op, BoundLiteralExpr literalExpr, out BoundLiteralExpr result)
    {
        result = null!;
        switch (op)
        {
            case TokenKind.Minus when literalExpr.Value.Kind == ConstantKind.Int:
                result = new BoundLiteralExpr(ConstantValue.FromInt(checked(-literalExpr.Value.Int)));
                return true;
            case TokenKind.Not when literalExpr.Value.Kind is ConstantKind.Bool:
                result = new BoundLiteralExpr(ConstantValue.FromBool(!literalExpr.Value.Bool));
                return true;
        }

        return false;
    }

    private static bool TryFoldBinary(TokenKind op, BoundLiteralExpr left, BoundLiteralExpr right,
        out BoundLiteralExpr result)
    {
        result = null!;
        // Int arithmetic/compare
        if (left.Value.Kind == ConstantKind.Int && right.Value.Kind == ConstantKind.Int)
        {
            var a =  left.Value.Int;
            var b = right.Value.Int;
            
            switch (op)
            {
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(ConstantValue.FromInt(checked(a + b)));
                    return true;
                case TokenKind.Minus:
                    result = new BoundLiteralExpr(ConstantValue.FromInt(checked(a - b)));
                    return true;
                case TokenKind.Star:
                    result = new BoundLiteralExpr(ConstantValue.FromInt(checked(a * b)));
                    return true;
                case TokenKind.Slash when b != 0:
                    result = new BoundLiteralExpr(ConstantValue.FromInt(checked(a / b)));
                    return true;
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a == b)));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a != b)));
                    return true;
                case TokenKind.Less:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a < b)));
                    return true;
                case TokenKind.LessEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a <= b)));
                    return true;
                case TokenKind.Greater:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a > b)));
                    return true;
                case TokenKind.GreaterEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a >= b)));
                    return true;
            }
        }
        
        // Real arithmetic/compare
        if (left.Value.Kind == ConstantKind.Real && right.Value.Kind == ConstantKind.Real)
        {
            var a = left.Value.Real;
            var b = right.Value.Real;
            
            switch (op)
            {
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(ConstantValue.FromReal(checked(a + b)));
                    return true;
                case TokenKind.Minus:
                    result = new BoundLiteralExpr(ConstantValue.FromReal(checked(a - b)));
                    return true;
                case TokenKind.Star:
                    result = new BoundLiteralExpr(ConstantValue.FromReal(checked(a * b)));
                    return true;
                case TokenKind.Slash when b != 0:
                    result = new BoundLiteralExpr(ConstantValue.FromReal(checked(a / b)));
                    return true;
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a == b)));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a != b)));
                    return true;
                case TokenKind.Less:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a < b)));
                    return true;
                case TokenKind.LessEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a <= b)));
                    return true;
                case TokenKind.Greater:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a > b)));
                    return true;
                case TokenKind.GreaterEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(checked(a >= b)));
                    return true;
            }
        }

        // Bool compare
        if (left.Value.Kind == ConstantKind.Bool && right.Value.Kind == ConstantKind.Bool)
        {
            var p = left.Value.Bool;
            var q =  right.Value.Bool;
            switch (op)
            {
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(p == q));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(p != q));
                    return true;
            }
        }

        // String concatenation/compare
        if (left.Value.Kind == ConstantKind.String && right.Value.Kind == ConstantKind.String)
        {
            var s1 = left.Value.String;
            var s2 = right.Value.String;
            
            switch (op)
            {
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(s1 == s2));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(ConstantValue.FromBool(s1 != s2));
                    return true;
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(ConstantValue.FromString(s1 + s2));
                    return true;
            }
        }

        return false;
    }

    private static bool TryFoldLogical(TokenKind op, BoundLiteralExpr left, BoundLiteralExpr right,
        out BoundLiteralExpr result)
    {
        result = null!;

        if (left.Value.Kind != ConstantKind.Bool ||
            right.Value.Kind != ConstantKind.Bool) return false;
        
        var p = left.Value.Bool;
        var q =  right.Value.Bool;
        switch (op)
        {
            case TokenKind.And:
                result = new BoundLiteralExpr(ConstantValue.FromBool(p && q));
                return true;
            case TokenKind.Or:
                result = new BoundLiteralExpr(ConstantValue.FromBool(p || q));
                return true;
        }

        return false;
    }
}