using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Rewriting;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Optimization;

public class ConstantFolder(BoundTree? boundTree) : BoundTreeRewriter
{
    readonly BoundTree? boundTree = boundTree;
    
    public BoundTree Fold()
    {
        if (boundTree is null) return new BoundTree([], "Empty bound tree");
        
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
            case TokenKind.And when left is BoundLiteralExpr { Token.Kind: TokenKind.LiteralTrue or TokenKind.LiteralFalse } l:
                return l.Token.Kind == TokenKind.LiteralTrue
                    ? right
                    : new BoundLiteralExpr(new Token(TokenKind.LiteralFalse, l.Token.Line));
            case TokenKind.Or when left is BoundLiteralExpr { Token.Kind: TokenKind.LiteralTrue or TokenKind.LiteralFalse } l:
                return l.Token.Kind == TokenKind.LiteralTrue
                    ? new BoundLiteralExpr(new Token(TokenKind.LiteralTrue, l.Token.Line))
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
        if (cond is BoundLiteralExpr { Token.Kind: TokenKind.LiteralTrue or TokenKind.LiteralFalse } literal)
        {
            return literal.Token.Kind == TokenKind.LiteralTrue ? thenS : elseS ?? new BoundBlockStmt([], new Scope());
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
        if (cond is BoundLiteralExpr { Token.Kind: TokenKind.LiteralFalse })
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
            case TokenKind.Minus when literalExpr.Token.Kind == TokenKind.LiteralInt:
                result = new BoundLiteralExpr(new Token(TokenKind.Int, literalExpr.Token.Line, checked(-literalExpr.Token.IntValue)));
                return true;
            case TokenKind.Not when literalExpr.Token.Kind is TokenKind.LiteralTrue or TokenKind.LiteralFalse:
                result = new BoundLiteralExpr(new Token(literalExpr.Token.Kind == TokenKind.LiteralTrue ? TokenKind.LiteralFalse : TokenKind.LiteralTrue, literalExpr.Token.Line));
                return true;
        }

        return false;
    }

    private static bool TryFoldBinary(TokenKind op, BoundLiteralExpr left, BoundLiteralExpr right,
        out BoundLiteralExpr result)
    {
        result = null!;
        // Int arithmetic/compare
        if (left.Token.Kind == TokenKind.LiteralInt && right.Token.Kind == TokenKind.LiteralInt)
        {
            var a =  left.Token.IntValue;
            var b = right.Token.IntValue;
            
            switch (op)
            {
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralInt, left.Token.Line, checked(a + b)));
                    return true;
                case TokenKind.Minus:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralInt, left.Token.Line, checked(a - b)));
                    return true;
                case TokenKind.Star:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralInt, left.Token.Line, checked(a * b)));
                    return true;
                case TokenKind.Slash when b != 0:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralInt, left.Token.Line, checked(a / b)));
                    return true;
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(new Token(a == b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(new Token(a != b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.Less:
                    result = new BoundLiteralExpr(new Token(a < b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.LessEqual:
                    result = new BoundLiteralExpr(new Token(a <= b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.Greater:
                    result = new BoundLiteralExpr(new Token(a > b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.GreaterEqual:
                    result = new BoundLiteralExpr(new Token(a >= b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
            }
        }
        
        // Real arithmetic/compare
        if (left.Token.Kind == TokenKind.LiteralReal && right.Token.Kind == TokenKind.LiteralReal)
        {
            var a =  left.Token.RealValue;
            var b = right.Token.RealValue;
            
            switch (op)
            {
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralReal, left.Token.Line, checked(a + b)));
                    return true;
                case TokenKind.Minus:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralReal, left.Token.Line, checked(a - b)));;
                    return true;
                case TokenKind.Star:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralReal, left.Token.Line, checked(a * b)));;
                    return true;
                case TokenKind.Slash when b != 0:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralReal, left.Token.Line, checked(a / b)));;
                    return true;
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(new Token(a == b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(new Token(a != b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.Less:
                    result = new BoundLiteralExpr(new Token(a < b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.LessEqual:
                    result = new BoundLiteralExpr(new Token(a <= b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.Greater:
                    result = new BoundLiteralExpr(new Token(a > b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.GreaterEqual:
                    result = new BoundLiteralExpr(new Token(a >= b ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
            }
        }

        // Bool compare
        if (left.Token.Kind is TokenKind.LiteralTrue or TokenKind.LiteralFalse && right.Token.Kind is TokenKind.LiteralTrue or TokenKind.LiteralFalse)
        {
            var p = left.Token.Kind == TokenKind.LiteralTrue;
            var q =  right.Token.Kind == TokenKind.LiteralTrue;
            switch (op)
            {
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(new Token(p == q ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(new Token(p != q ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
            }
        }

        // String concatenation/compare
        if (left.Token.Kind == TokenKind.LiteralString && right.Token.Kind == TokenKind.LiteralString)
        {
            var s1 = left.Token.StringValue;
            var s2 = right.Token.StringValue;
            
            switch (op)
            {
                case TokenKind.Equal:
                    result = new BoundLiteralExpr(new Token(s1 == s2 ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.NotEqual:
                    result = new BoundLiteralExpr(new Token(s1 != s2 ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                    return true;
                case TokenKind.Plus:
                    result = new BoundLiteralExpr(new Token(TokenKind.LiteralString, left.Token.Line, s1 + s2));
                    return true;
            }
        }

        return false;
    }

    private static bool TryFoldLogical(TokenKind op, BoundLiteralExpr left, BoundLiteralExpr right,
        out BoundLiteralExpr result)
    {
        result = null!;

        if (left.Token.Kind is not (TokenKind.LiteralTrue or TokenKind.LiteralFalse) ||
            right.Token.Kind is not (TokenKind.LiteralTrue or TokenKind.LiteralFalse)) return false;
        
        var p = left.Token.Kind == TokenKind.LiteralTrue;
        var q =  right.Token.Kind == TokenKind.LiteralTrue;
        switch (op)
        {
            case TokenKind.And:
                result = new BoundLiteralExpr(new Token(p && q ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                return true;
            case TokenKind.Or:
                result = new BoundLiteralExpr(new Token(p || q ? TokenKind.LiteralTrue : TokenKind.LiteralFalse, left.Token.Line));
                return true;
        }

        return false;
    }
}