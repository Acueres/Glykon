using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR;

public class IRTypeBuilder(
    BoundTree boundTree,
    TypeSystem typeSystem,
    IdentifierInterner interner,
    string fileName)
{
    readonly List<IGlykonError> errors = [];

    public (IRTree, IGlykonError[]) Build()
    {
        List<IRStatement> irStatements = new(boundTree.Length);
        irStatements.AddRange(boundTree.Select(BuildStatement));

        IRTree irTree = new([..irStatements], fileName);

        return (irTree, [..errors]);
    }

    IRStatement BuildStatement(BoundStatement stmt)
    {
        switch (stmt.Kind)
        {
            case BoundStatementKind.Block:
            {
                var blockStmt = (BoundBlockStmt)stmt;
                var irStatements = blockStmt.Statements.Select(BuildStatement).ToArray();
                IRBlockStmt irBlockStmt = new([.. irStatements], blockStmt.Scope);
                return irBlockStmt;
            }
            case BoundStatementKind.If:
            {
                var ifStmt = (BoundIfStmt)stmt;
                var irThenStmt = BuildStatement(ifStmt.ThenStatement);
                var irElseStmt = ifStmt.ElseStatement is null ? null : BuildStatement(ifStmt.ElseStatement);

                var irCondition = BuildExpression(ifStmt.Condition);
                CheckCondition(irCondition);

                IRIfStmt irIfStmt = new(irCondition, irThenStmt, irElseStmt);
                return irIfStmt;
            }
            case BoundStatementKind.While:
            {
                var whileStmt = (BoundWhileStmt)stmt;
                var irStatement = BuildStatement(whileStmt.Body);

                var irCondition = BuildExpression(whileStmt.Condition);
                CheckCondition(irCondition);

                var irWhileStmt = new IRWhileStmt(irCondition, irStatement);

                return irWhileStmt;
            }
            case BoundStatementKind.Variable:
            {
                var variableStmt = (BoundVariableDeclaration)stmt;
                var declaredType = variableStmt.DeclaredType;
                var initializer = BuildExpression(variableStmt.Initializer);

                var type = initializer.Type;

                if (declaredType.Kind == TypeKind.None)
                {
                    variableStmt.Symbol.UpdateType(type);
                }
                else if (TypeSystem.CanImplicitlyConvert(type, declaredType))
                {
                    initializer = new IRConversionExpr(initializer, declaredType);
                }
                else if (type != declaredType)
                {
                    TypeError error = new(fileName,
                        $"Type mismatch between {interner[declaredType.NameId]} and {interner[type.NameId]}");
                    errors.Add(error);
                }

                return new IRVariableDeclaration(initializer, variableStmt.Symbol);
            }
            case BoundStatementKind.Constant:
            {
                var constantStmt = (BoundConstantDeclaration)stmt;
                var declaredType = constantStmt.Symbol.Type;
                
                var initializer = BuildExpression(constantStmt.Initializer);

                if (initializer.Kind == IRExpressionKind.Invalid) return new IRConstantDeclaration(initializer, constantStmt.Symbol);
                    if (!declaredType.IsPrimitive)
                {
                    TypeError error = new(fileName,
                        $"Wrong constant type: {interner[declaredType.NameId]}. Must be compile-time");
                    errors.Add(error);
                    return new IRInvalidStmt();
                }

                if (TypeSystem.CanImplicitlyConvert(declaredType, initializer.Type))
                {
                    initializer = new IRConversionExpr(initializer, declaredType);
                }

                CheckConstantType(initializer, constantStmt.Symbol.Type);

                return new IRConstantDeclaration(initializer, constantStmt.Symbol);
            }

            case BoundStatementKind.Function:
            {
                var functionStmt = (BoundFunctionDeclaration)stmt;
                var irStatements = functionStmt.Body.Statements.Select(BuildStatement).ToArray();
                IRBlockStmt irBody = new([.. irStatements], functionStmt.Body.Scope);
                return new IRFunctionDeclaration(functionStmt.Signature, functionStmt.Parameters,
                    functionStmt.ReturnType, irBody);
            }
            case BoundStatementKind.Return:
            {
                var returnStmt = (BoundReturnStmt)stmt;

                if (returnStmt.ContainingFunction is null) return new IRInvalidStmt();
                
                var returnType = returnStmt.ContainingFunction.Type;
                var value = returnStmt.Value is null ? null : BuildExpression(returnStmt.Value);

                if (value is not null && TypeSystem.CanImplicitlyConvert(value.Type, returnType))
                {
                    value = new IRConversionExpr(value, returnType);
                }
                
                CheckReturnStatementType(value, returnType);
                return new IRReturnStmt(value, returnStmt.Token);
            }
            case BoundStatementKind.Expression:
            {
                var expression = (BoundExpressionStmt)stmt;
                var irExpression = BuildExpression(expression.Expression);
                return new IRExpressionStmt(irExpression);
            }
            case BoundStatementKind.Jump:
            {
                var jumpStmt = (BoundJumpStmt)stmt;
                return new IRJumpStmt(jumpStmt.Token);
            }
            default: return new IRInvalidStmt();
        }
    }

    IRExpression BuildExpression(BoundExpression expression)
    {
        switch (expression.Kind)
        {
            case BoundExpressionKind.Literal:
            {
                var literalExpr = (BoundLiteralExpr)expression;
                var type = typeSystem[literalExpr.Value.Kind];
                return new IRLiteralExpr(literalExpr.Value, type);
            }
            case BoundExpressionKind.Unary:
            {
                var unaryExpr = (BoundUnaryExpr)expression;
                var operand = BuildExpression(unaryExpr.Operand);

                if (operand.Type.IsError)
                {
                    return new IRInvalidExpr(typeSystem[TypeKind.Error]);
                }

                var type = unaryExpr.Operator.Kind == TokenKind.Not ? typeSystem[TypeKind.Bool] : operand.Type;
                var op = TokenOpMap.ToUnaryOp(unaryExpr.Operator.Kind);
                    
                var irUnary = new IRUnaryExpr(op, operand, type);
                CheckUnaryExpression(irUnary);
                return irUnary;
            }
            case BoundExpressionKind.Binary:
            {
                var binaryExpr = (BoundBinaryExpr)expression;
                var op = TokenOpMap.ToBinaryOp(binaryExpr.Operator.Kind);
                var left = BuildExpression(binaryExpr.Left);
                var right = BuildExpression(binaryExpr.Right);

                if (left.Type.IsError || right.Type.IsError)
                {
                    return new IRInvalidExpr(typeSystem[TypeKind.Error]);
                }
                
                // Handle string concatenation
                if (op == BinaryOp.Add && left.Type.Kind == TypeKind.String && right.Type.Kind == TypeKind.String)
                {
                    
                    return new IRBinaryExpr(op, left, right, typeSystem[TypeKind.String]);
                }

                if (OpTraits.IsArithmetic(op))
                {
                    if (!PromoteNumericPair(ref left, ref right, out var type))
                    {
                        return BinaryInvalid(op, left, right);
                    }
                    
                    return new IRBinaryExpr(op, left, right, type!);
                }

                if (OpTraits.IsComparison(op))
                {
                    // <, <=, >, >=
                    if (!PromoteNumericPair(ref left, ref right, out _))
                    {
                        return BinaryInvalid(op, left, right);
                    }

                    return new IRBinaryExpr(op, left, right, typeSystem[TypeKind.Bool]);
                }

                if (OpTraits.IsEquality(op))
                {
                    // ==, !=
                    if (!PromoteForEquality(ref left, ref right))
                    {
                        return BinaryInvalid(op, left, right);
                    }

                    return new IRBinaryExpr(op, left, right, typeSystem[TypeKind.Bool]);
                }

                return BinaryInvalid(op, left, right);
            }
            case BoundExpressionKind.Logical:
            {
                var logicalExpr = (BoundLogicalExpr)expression;
                var left = BuildExpression(logicalExpr.Left);
                var right = BuildExpression(logicalExpr.Right);
                
                var op = TokenOpMap.ToBinaryOp(logicalExpr.Operator.Kind);

                var irLogical = new IRLogicalExpr(op, left, right, typeSystem[TypeKind.Bool]);

                CheckLogicalExpression(irLogical);

                return irLogical;
            }
            case BoundExpressionKind.Assignment:
            {
                var assignmentExpr = (BoundAssignmentExpr)expression;
                var symbol = assignmentExpr.Symbol;
                var value = BuildExpression(assignmentExpr.Value);
                
                if (value.Type.IsError) return new IRAssignmentExpr(value, assignmentExpr.Symbol);

                if (TypeSystem.CanImplicitlyConvert(value.Type, symbol.Type))
                {
                    value = new IRConversionExpr(value, symbol.Type);
                }
                
                var irAssignment = new IRAssignmentExpr(value, symbol);
                CheckAssignmentExpression(irAssignment);
                return irAssignment;
            }
            case BoundExpressionKind.Variable:
            {
                var variableExpr = (BoundVariableExpr)expression;
                var symbol = variableExpr.Symbol;
                return new IRVariableExpr(symbol);
            }
            case BoundExpressionKind.Grouping:
            {
                var groupExpr = (BoundGroupingExpr)expression;
                var boundExpression = BuildExpression(groupExpr.Expression);
                return new IRGroupingExpr(boundExpression);
            }
            case BoundExpressionKind.Call:
            {
                var callExpr = (BoundCallExpr)expression;

                var parameters = callExpr.Parameters.Select(BuildExpression).ToArray();
                var paramTypes = parameters.Select(arg => arg.Type).ToArray();

                var overloads = callExpr.Overloads;
                var function = overloads.FirstOrDefault(overload => overload.Parameters.SequenceEqual(paramTypes));

                if (function is null)
                {
                    errors.Add(new TypeError(fileName, $"Cannot resolve function {interner[callExpr.NameId]}"));
                    return new IRInvalidExpr(typeSystem[TypeKind.Error]);
                }

                return new IRCallExpr(function, parameters);
            }
            case BoundExpressionKind.Conversion:
            {
                var conversionExpr = (BoundConversionExpr)expression;
                var irExpression = BuildExpression(conversionExpr.Expression);
                return new IRConversionExpr(irExpression, conversionExpr.TargetType);
            }
            default: return new IRInvalidExpr(typeSystem[TypeKind.Error]);
        }
    }

    void CheckConstantType(IRExpression initializer, TypeSymbol declaredType)
    {
        var initializerType = initializer.Type;
        if (initializerType != declaredType)
        {
            TypeError error = new(fileName,
                $"Type mismatch between {interner[declaredType.NameId]} and {interner[initializerType.NameId]}");
            errors.Add(error);
        }
    }

    void CheckUnaryExpression(IRUnaryExpr unaryExpr)
    {
        var operandType = unaryExpr.Type;

        switch (unaryExpr.Operator)
        {
            case UnaryOp.LogicalNot when operandType.Kind != TypeKind.Bool:
            case UnaryOp.Minus when !operandType.IsNumeric:
            {
                TypeError error = new(fileName,
                    $"Operator {unaryExpr.Operator} cannot be applied to operand type '{interner[operandType.NameId]}'");
                errors.Add(error);
                break;
            }
        }
    }

    void CheckLogicalExpression(IRLogicalExpr logicalExpr)
    {
        var leftType = logicalExpr.Left.Type;
        var rightType = logicalExpr.Right.Type;

        if (leftType.Kind != TypeKind.Bool || rightType.Kind != TypeKind.Bool)
        {
            errors.Add(new TypeError(fileName,
                $"Type mismatch; operator {logicalExpr.Operator} cannot be applied between types {interner[leftType.NameId]} and {interner[rightType.NameId]}"));
        }
    }

    void CheckAssignmentExpression(IRAssignmentExpr assignmentExpr)
    {
        var variableType = assignmentExpr.Symbol.Type;
        var valueType = assignmentExpr.Value.Type;

        if (variableType != valueType)
        {
            errors.Add(new TypeError(fileName,
                $"Type mismatch; can't assign {interner[valueType.NameId]} to {interner[variableType.NameId]}"));
        }
    }

    void CheckCondition(IRExpression condition)
    {
        var conditionType = condition.Type;
        if (conditionType.Kind != TypeKind.Bool)
        {
            errors.Add(new TypeError($"Type mismatch: condition must be bool, but got {interner[conditionType.NameId]}",
                fileName));
        }
    }

    void CheckReturnStatementType(IRExpression? expression, TypeSymbol expected)
    {
        TypeError error;
        switch (expression)
        {
            case null when !expected.IsNone:
                error = new TypeError(fileName,
                    $"Type mismatch. Expected {interner[expected.NameId]}, got none");
                errors.Add(error);
                return;
            case null:
                return;
        }

        var actual = expression.Type;
        if (expected == actual) return;

        error = new TypeError(fileName,
            $"Type mismatch. Expected {interner[expected.NameId]}, got {interner[actual.NameId]}");
        errors.Add(error);
    }

    bool IsArithmeticOperator(TokenKind operatorKind)
    {
        return operatorKind is TokenKind.Plus or TokenKind.Minus or TokenKind.Star or TokenKind.Slash;
    }

    bool IsComparisonOperator(TokenKind operatorKind)
    {
        return operatorKind is TokenKind.Greater or TokenKind.GreaterEqual or TokenKind.Less or TokenKind.LessEqual;
    }

    bool IsEqualityOperator(TokenKind operatorKind)
    {
        return operatorKind is TokenKind.Equal or TokenKind.NotEqual;
    }

    bool PromoteNumericPair(ref IRExpression l, ref IRExpression r, out TypeSymbol? common)
    {
        if (!l.Type.IsNumeric || !r.Type.IsNumeric)
        {
            common = null;
            return false;
        }

        common = typeSystem.GetCommonNumericType(l.Type, r.Type);
        if (l.Type != common) l = new IRConversionExpr(l, common);
        if (r.Type != common) r = new IRConversionExpr(r, common);
        return true;
    }

    bool PromoteForEquality(ref IRExpression l, ref IRExpression r)
    {
        if (l.Type.IsNumeric && r.Type.IsNumeric) return PromoteNumericPair(ref l, ref r, out _);
        if (l.Type.Kind == TypeKind.Bool && r.Type.Kind == TypeKind.Bool) return true;
        if (l.Type.Kind == TypeKind.String && r.Type.Kind == TypeKind.String) return true;
        return false;
    }

    IRExpression BinaryInvalid(BinaryOp op, IRExpression l, IRExpression r)
    {
        errors.Add(new TypeError(fileName,
            $"Operator {op} cannot be applied between types '{interner[l.Type.NameId]}' and '{interner[r.Type.NameId]}'"));
        return new IRInvalidExpr(typeSystem[TypeKind.Error]);
    }
}
