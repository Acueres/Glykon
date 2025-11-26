using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Rewriting;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Lowering;

public class Lowerer(IRTree ir, IdentifierInterner interner, TypeSystem ts, SymbolTable st, LanguageMode mode) : IRTreeRewriter
{
    public IRTree Lower()
    {
        List<IRStatement> rewritten = new(ir.Length);
        rewritten.AddRange(ir.Select(VisitStmt));

        List<IRStatement> wrapped = mode == LanguageMode.Script
            ? WrapScript(rewritten)
            : rewritten;

        return new IRTree([..wrapped], ir.FileName);
    }
    
    protected override IRStatement RewriteFor(IRForStmt forStmt)
    {
        var iteratorDecl = (IRVariableDeclaration)VisitStmt(forStmt.Iterator);
        var rangeExpr = (IRRangeExpr)VisitExpr(forStmt.Range);
        var bodyBlock = (IRBlockStmt)VisitStmt(forStmt.Body);
        
        var normalizedFor = new IRForStmt(iteratorDecl, rangeExpr, bodyBlock);
        
        var whileStmt = LowerForToWhile(normalizedFor);
        
        IRStatement[] stmts = [iteratorDecl, whileStmt];
        return new IRBlockStmt(stmts, bodyBlock.Scope);
    }

    IRWhileStmt LowerForToWhile(IRForStmt forStatement)
    {
        var range = forStatement.Range;
        
        var iteratorVariable = new IRVariableExpr(forStatement.Iterator.Symbol);

        var stepExpr = range.Step switch
        {
            null => new IRLiteralExpr(ConstantValue.FromInt(1), ts[TypeKind.Int64]),
            IRLiteralExpr stepLiteral => stepLiteral,
            _ => range.Step!
        };

        IRExpression loopCondition = HandleForDirection(range, stepExpr, iteratorVariable);
        var nextIterator = new IRBinaryExpr(BinaryOp.Add, iteratorVariable, stepExpr, ts[TypeKind.Int64]);
        var iteratorIncrement = new IRAssignmentExpr(nextIterator, iteratorVariable.Symbol);

        var body = (IRBlockStmt)forStatement.Body;
        var bodyStatements = body.Statements.ToList();
        bodyStatements.Add(new IRExpressionStmt(iteratorIncrement));

        return new IRWhileStmt(loopCondition, new IRBlockStmt([..bodyStatements], body.Scope));
    }

    IRExpression HandleForDirection(
        IRRangeExpr range,
        IRExpression stepExpr,
        IRVariableExpr iteratorVariable)
    {
        var intType = ts[TypeKind.Int64];
        var boolType = ts[TypeKind.Bool];
        
        // Step is a literal: we know its sign now
        if (stepExpr is IRLiteralExpr stepLiteral)
        {
            var stepValue = stepLiteral.Value.Int;

            // Step 0: empty range
            if (stepValue == 0)
            {
                return new IRLiteralExpr(ConstantValue.FromBool(false), boolType);
            }

            // Positive = ascending; negative = descending
            var comparisonOp =
                stepValue > 0
                    ? (range.IsInclusive ? BinaryOp.LessOrEqual : BinaryOp.Less)
                    : (range.IsInclusive ? BinaryOp.GreaterOrEqual : BinaryOp.Greater);

            return new IRBinaryExpr(
                comparisonOp,
                iteratorVariable,
                range.End,
                boolType);
        }

        // Dynamic step: sign only known at runtime
        var zeroLiteral = new IRLiteralExpr(ConstantValue.FromInt(0), intType);

        // step > 0
        var stepGreaterZero = new IRBinaryExpr(
            BinaryOp.Greater,
            stepExpr,
            zeroLiteral,
            boolType);

        // step < 0
        var stepLessZero = new IRBinaryExpr(
            BinaryOp.Less,
            stepExpr,
            zeroLiteral,
            boolType);

        // Ascending bounds: i < end (or <=)
        var forwardBound = new IRBinaryExpr(
            range.IsInclusive ? BinaryOp.LessOrEqual : BinaryOp.Less,
            iteratorVariable,
            range.End,
            boolType);

        // Descending bounds: i > end (or >=)
        var backwardBound = new IRBinaryExpr(
            range.IsInclusive ? BinaryOp.GreaterOrEqual : BinaryOp.Greater,
            iteratorVariable,
            range.End,
            boolType);

        // (step > 0 && forwardBound) || (step < 0 && backwardBound)
        var forwardCond = new IRLogicalExpr(
            BinaryOp.LogicalAnd,
            stepGreaterZero,
            forwardBound,
            boolType);

        var backwardCond = new IRLogicalExpr(
            BinaryOp.LogicalAnd,
            stepLessZero,
            backwardBound,
            boolType);

        return new IRLogicalExpr(
            BinaryOp.LogicalOr,
            forwardCond,
            backwardCond,
            boolType);
    }

    private List<IRStatement> WrapScript(List<IRStatement> stmts)
    {
        List<IRStatement> functions = [];
        List<IRStatement> constants = [];
        List<IRStatement> scriptStatements = [];

        foreach (var stmt in stmts)
        {
            if (stmt is IRFunctionDeclaration f)
            {
                functions.Add(f);
            }
            else if (stmt is IRConstantDeclaration c)
            {
                constants.Add(c);
            }
            else
            {
                scriptStatements.Add(stmt);
            }
        }

        var existingMain = functions
            .OfType<IRFunctionDeclaration>()
            .FirstOrDefault(f => interner[f.Signature.NameId] == "main");

        if (existingMain != null)
        {
            throw new InvalidOperationException(
                "Script mode cannot contain both top-level statements and a 'main' function definition.");
        }
        
        st.ResetScope();
        var mainSymbol = st.RegisterFunction("main", ts[TypeKind.None], []);
        if (mainSymbol == null)
        {
            throw new InvalidOperationException("Failed to register synthetic main function.");
        }
        
        var bodyBlock = new IRBlockStmt([..scriptStatements], st.GetCurrentScope());
        var mainDeclaration = new IRFunctionDeclaration(mainSymbol, [], mainSymbol.Type, bodyBlock);
        
        functions.Add(mainDeclaration);
        
        return [..constants, ..functions];
    }
}