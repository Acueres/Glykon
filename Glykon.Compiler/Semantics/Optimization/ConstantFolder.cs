using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Rewriting;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Optimization;

public class ConstantFolder(IRTree irTree, TypeSystem typeSystem, IdentifierInterner interner, string filename): IRTreeRewriter
{
    private readonly List<ConstantFoldingError> errors = [];
    
    public (IRTree, IGlykonError[]) Fold()
    {
        List<IRStatement> rewritten = new(irTree.Length);
        rewritten.AddRange(irTree.Select(VisitStmt));
        return (new IRTree([..rewritten], irTree.FileName), [..errors]);
    }
    
    protected override IRExpression RewriteUnary(IRUnaryExpr unaryExpr)
    {
        var opnd = VisitExpr(unaryExpr.Operand);
        if (!ReferenceEquals(opnd, unaryExpr.Operand)) unaryExpr = new IRUnaryExpr(unaryExpr.Operator, opnd, unaryExpr.Type);
        if (opnd is not IRLiteralExpr lit) return unaryExpr;
        if (TryFoldUnary(unaryExpr.Operator, lit, out var folded)) return folded;

        return unaryExpr;
    }
    
    protected override IRExpression RewriteBinary(IRBinaryExpr binaryExpr)
    {
        var left = VisitExpr(binaryExpr.Left);
        var right = VisitExpr(binaryExpr.Right);
        if (!ReferenceEquals(left, binaryExpr.Left) || !ReferenceEquals(right, binaryExpr.Right))
            binaryExpr = new IRBinaryExpr(binaryExpr.Operator, left, right, binaryExpr.Type);
        
        if (left is not IRLiteralExpr l1 || right is not IRLiteralExpr l2) return binaryExpr;
        if (TryFoldBinary(binaryExpr.Operator, l1, l2, out var folded)) return folded;

        return binaryExpr;
    }
    
    protected override IRExpression RewriteLogical(IRLogicalExpr logicalExpr)
    {
        var left = VisitExpr(logicalExpr.Left);
        var right = VisitExpr(logicalExpr.Right);
        if (!ReferenceEquals(left, logicalExpr.Left) || !ReferenceEquals(right, logicalExpr.Right))
            logicalExpr = new IRLogicalExpr(logicalExpr.Operator, left, right, logicalExpr.Type);

        switch (logicalExpr.Operator)
        {
            // Short-circuit boolean ops
            case BinaryOp.LogicalAnd when left is IRLiteralExpr { Value.Kind: ConstantKind.Bool } l:
                return l.Value.Bool
                    ? right
                    : new IRLiteralExpr(l.Value, typeSystem[TypeKind.Bool]);
            case BinaryOp.LogicalOr when left is IRLiteralExpr { Value.Kind: ConstantKind.Bool } l:
                return l.Value.Bool
                    ? new IRLiteralExpr(l.Value, typeSystem[TypeKind.Bool])
                    : right;
        }
        
        if (left is not IRLiteralExpr l1 || right is not IRLiteralExpr l2) return logicalExpr;
        if (TryFoldLogical(logicalExpr.Operator, l1, l2, out var folded)) return folded;
        
        return logicalExpr;
    }

    protected override IRStatement RewriteConstantDeclaration(IRConstantDeclaration c)
    {
        var foldedInitializer = VisitExpr(c.Initializer);

        if (foldedInitializer is IRLiteralExpr literal)
        {
            c.Symbol.Value = literal.Value;
        }
        else
        {
            var error = new ConstantFoldingError(filename,
                $"Initializer of constant {interner[c.Symbol.NameId]} is not a constant expression");
            errors.Add(error);
        }

        return ReferenceEquals(foldedInitializer, c.Initializer)
            ? c
            : new IRConstantDeclaration(foldedInitializer, c.Symbol);
    }

    protected override IRExpression RewriteVariable(IRVariableExpr v)
    {
        if (v.Symbol is ConstantSymbol c && !c.Value.IsNone)
        {
            return new IRLiteralExpr(c.Value, v.Type);
        }
        
        return v;
    }

    protected override IRStatement RewriteIf(IRIfStmt ifStmt)
    {
        var cond = VisitExpr(ifStmt.Condition);
        var thenS = VisitStmt(ifStmt.ThenStatement);
        var elseS = ifStmt.ElseStatement is null ? null : VisitStmt(ifStmt.ElseStatement);
        if (cond is IRLiteralExpr { Value.Kind: ConstantKind.Bool } literal)
        {
            return literal.Value.Bool ? thenS : elseS ?? new IRBlockStmt([], new Scope());
        }

        if (ReferenceEquals(cond, ifStmt.Condition) && ReferenceEquals(thenS, ifStmt.ThenStatement) &&
            ReferenceEquals(elseS, ifStmt.ElseStatement))
            return ifStmt;
        
        return new IRIfStmt(cond, thenS, elseS);
    }

    protected override IRStatement RewriteWhile(IRWhileStmt whileStmt)
    {
        var cond = VisitExpr(whileStmt.Condition);
        var body = VisitStmt(whileStmt.Body);
        if (cond is IRLiteralExpr { Value.Kind: ConstantKind.Bool, Value.Bool: false })
        {
            return new IRBlockStmt([], new Scope());
        }

        if (ReferenceEquals(cond, whileStmt.Condition) && ReferenceEquals(body, whileStmt.Body)) return whileStmt;
        return new IRWhileStmt(cond, body);
    }
    
    protected override IRExpression RewriteConversion(IRConversionExpr cnv)
    {
        var inner = VisitExpr(cnv.Expression);
        if (inner is IRLiteralExpr lit)
        {
            //identity
            if (cnv.Type == lit.Type)
            {
                return lit;
            }
            // int to float
            if (cnv.Type.Kind == TypeKind.Float64 && lit.Value.Kind == ConstantKind.Int)
            {
                return new IRLiteralExpr(ConstantValue.FromReal(lit.Value.Int), typeSystem[TypeKind.Float64]);
            }
            
            var error = new ConstantFoldingError(filename,
                $"Cannot convert constant from {interner[lit.Type.NameId]} to {interner[cnv.Type.NameId]}");
            errors.Add(error);
        }

        return ReferenceEquals(inner, cnv.Expression) ? cnv : new IRConversionExpr(inner, cnv.Type);
    }

    // Literal evaluation helpers
    private bool TryFoldUnary(UnaryOp op, IRLiteralExpr literalExpr, out IRLiteralExpr result)
    {
        result = null!;
        switch (op)
        {
            case UnaryOp.Minus when literalExpr.Value.Kind == ConstantKind.Int:
                result = new IRLiteralExpr(ConstantValue.FromInt(checked(-literalExpr.Value.Int)), typeSystem[TypeKind.Int64]);
                return true;
            case UnaryOp.LogicalNot when literalExpr.Value.Kind is ConstantKind.Bool:
                result = new IRLiteralExpr(ConstantValue.FromBool(!literalExpr.Value.Bool), typeSystem[TypeKind.Bool]);
                return true;
        }

        return false;
    }

    private bool TryFoldBinary(BinaryOp op, IRLiteralExpr left, IRLiteralExpr right,
        out IRLiteralExpr result)
    {
        result = null!;
        // Int arithmetic/compare
        if (left.Value.Kind == ConstantKind.Int && right.Value.Kind == ConstantKind.Int)
        {
            var a =  left.Value.Int;
            var b = right.Value.Int;

            switch (op)
            {
                case BinaryOp.Add:
                    try
                    {
                        result = new IRLiteralExpr(ConstantValue.FromInt(checked(a + b)), typeSystem[TypeKind.Int64]);
                    }
                    catch (OverflowException)
                    {
                        var error = new ConstantFoldingError(filename,
                            "Integer overflow in constant expression");
                        errors.Add(error);
                        return false;
                    }

                    return true;
                case BinaryOp.Subtract:
                    try
                    {
                        result = new IRLiteralExpr(ConstantValue.FromInt(checked(a - b)), typeSystem[TypeKind.Int64]);
                    }
                    catch (OverflowException)
                    {
                        var error = new ConstantFoldingError(filename,
                            "Integer overflow in constant expression");
                        errors.Add(error);
                        return false;
                    }
                    return true;
                case BinaryOp.Multiply:
                    try
                    {
                        result = new IRLiteralExpr(ConstantValue.FromInt(checked(a * b)), typeSystem[TypeKind.Int64]);
                    }
                    catch (OverflowException)
                    {
                        var error = new ConstantFoldingError(filename,
                            "Integer overflow in constant expression");
                        errors.Add(error);
                        return false;
                    }
                    return true;
                case BinaryOp.Divide when b != 0:
                    result = new IRLiteralExpr(ConstantValue.FromInt(a / b), typeSystem[TypeKind.Int64]);
                    return true;
                case BinaryOp.Divide:
                    var zeroDivisionError = new ConstantFoldingError(filename,
                        "Division by zero in constant expression");
                    errors.Add(zeroDivisionError);
                    return false;
                case BinaryOp.Equal:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a == b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.NotEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a != b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.Less:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a < b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.LessOrEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a <= b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.Greater:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a > b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.GreaterOrEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a >= b), typeSystem[TypeKind.Bool]);
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
                case BinaryOp.Add:
                    result = new IRLiteralExpr(ConstantValue.FromReal(a + b), typeSystem[TypeKind.Float64]);
                    return true;
                case BinaryOp.Subtract:
                    result = new IRLiteralExpr(ConstantValue.FromReal(a - b), typeSystem[TypeKind.Float64]);
                    return true;
                case BinaryOp.Multiply:
                    result = new IRLiteralExpr(ConstantValue.FromReal(a * b), typeSystem[TypeKind.Float64]);
                    return true;
                case BinaryOp.Divide when b != 0:
                    result = new IRLiteralExpr(ConstantValue.FromReal(a / b), typeSystem[TypeKind.Float64]);
                    return true;
                case BinaryOp.Divide:
                    var zeroDivisionError = new ConstantFoldingError(filename,
                        "Division by zero in constant expression");
                    errors.Add(zeroDivisionError);
                    return false;
                case BinaryOp.Equal:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a == b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.NotEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a != b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.Less:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a < b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.LessOrEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a <= b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.Greater:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a > b), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.GreaterOrEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(a >= b), typeSystem[TypeKind.Bool]);
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
                case BinaryOp.Equal:
                    result = new IRLiteralExpr(ConstantValue.FromBool(p == q), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.NotEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(p != q), typeSystem[TypeKind.Bool]);
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
                case BinaryOp.Equal:
                    result = new IRLiteralExpr(ConstantValue.FromBool(s1 == s2), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.NotEqual:
                    result = new IRLiteralExpr(ConstantValue.FromBool(s1 != s2), typeSystem[TypeKind.Bool]);
                    return true;
                case BinaryOp.Add:
                    result = new IRLiteralExpr(ConstantValue.FromString(s1 + s2),typeSystem[TypeKind.String]);
                    return true;
            }
        }

        return false;
    }

    private bool TryFoldLogical(BinaryOp op, IRLiteralExpr left, IRLiteralExpr right,
        out IRLiteralExpr result)
    {
        result = null!;

        if (left.Value.Kind != ConstantKind.Bool ||
            right.Value.Kind != ConstantKind.Bool) return false;
        
        var p = left.Value.Bool;
        var q =  right.Value.Bool;
        switch (op)
        {
            case BinaryOp.LogicalAnd:
                result = new IRLiteralExpr(ConstantValue.FromBool(p && q), typeSystem[TypeKind.Bool]);
                return true;
            case BinaryOp.LogicalOr:
                result = new IRLiteralExpr(ConstantValue.FromBool(p || q), typeSystem[TypeKind.Bool]);
                return true;
        }

        return false;
    }
}