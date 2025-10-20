using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.Types;

public class TypeChecker(string fileName, TypeSystem typeSystem, IdentifierInterner interner)
{
    readonly string fileName = fileName;
    readonly TypeSystem typeSystem = typeSystem;
    private readonly IdentifierInterner interner = interner;
    readonly List<IGlykonError> errors = [];

    public List<IGlykonError> GetErrors() => errors;

    public TypeSymbol CheckDeclaredType(BoundExpression expression, TypeSymbol declaredType)
    {
        var inferredType = InferType(expression);

        if (declaredType.Kind == TypeKind.None) return inferredType;
        if (declaredType != inferredType)
        {
            TypeError error = new(fileName, $"Type mismatch between {interner[declaredType.NameId]} and {interner[inferredType.NameId]}");
            errors.Add(error);
            return typeSystem[TypeKind.None];
        }

        return inferredType;
    }

    public void ValidateParameters(IEnumerable<BoundExpression> parameters,  IEnumerable<TypeSymbol> types)
    {
        foreach (var (parameter, type) in parameters.Zip(types))
        {
            var inferredType = InferType(parameter);
            if (type != inferredType)
            {
                TypeError error = new(fileName, $"Type mismatch between {interner[type.NameId]} and {interner[inferredType.NameId]}");
                errors.Add(error);
            }
        }
    }

    public void ValidateCondition(BoundExpression condition)
    {
        var conditionType = InferType(condition);
        if (conditionType.Kind != TypeKind.Bool)
        {
            errors.Add(new TypeError($"Type mismatch: condition must be bool, but got {interner[conditionType.NameId]}", fileName));
        }
    }

    public void ValidateReturnStatementType(BoundExpression expression, TypeSymbol expected)
    {
        var actual = InferType(expression);
        if (expected != actual)
        {
            TypeError error = new(fileName, $"Type mismatch. Expected {interner[expected.NameId]}, got {interner[actual.NameId]}");
            errors.Add(error);
        }
    }

    public TypeSymbol InferType(BoundExpression? expression)
    {
        if (expression is null) return typeSystem[TypeKind.None];

        switch (expression.Kind)
        {
            case ExpressionKind.Literal:
                {
                    var literalType = ((BoundLiteralExpr)expression).Value.Kind;
                    return literalType switch
                    {
                        ConstantKind.Int => typeSystem[TypeKind.Int64],
                        ConstantKind.Real => typeSystem[TypeKind.Float64],
                        ConstantKind.String => typeSystem[TypeKind.String],
                        ConstantKind.Bool => typeSystem[TypeKind.Bool],
                        _ => typeSystem[TypeKind.None],
                    };
                }
            case ExpressionKind.Unary:
                {
                    var unaryExpr = (BoundUnaryExpr)expression;
                    var operandType = InferType(unaryExpr.Operand);

                    if (operandType.Kind == TypeKind.None) return typeSystem[TypeKind.None];

                    if (unaryExpr.Operator.Kind == TokenKind.Not)
                    {
                        if (operandType.Kind != TypeKind.Bool)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Kind} cannot be applied to operand type '{interner[operandType.NameId]}'");
                            errors.Add(error);
                            return typeSystem[TypeKind.None];
                        }

                        return typeSystem[TypeKind.Bool];
                    }

                    if (unaryExpr.Operator.Kind == TokenKind.Minus)
                    {
                        if (operandType.Kind != TypeKind.Int64 && operandType.Kind != TypeKind.Float64)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Kind} cannot be applied to operand type '{interner[operandType.NameId]}'");
                            errors.Add(error);
                            return typeSystem[TypeKind.None];
                        }
                    }

                    return operandType;
                }
            case ExpressionKind.Binary:
                {
                    var binaryExpr = (BoundBinaryExpr)expression;
                    var leftType = InferType(binaryExpr.Left);
                    var rightType = InferType(binaryExpr.Right);

                    if (leftType.Kind == TypeKind.None || rightType.Kind == TypeKind.None)
                    {
                        return typeSystem[TypeKind.None];
                    }

                    if (leftType != rightType)
                    {
                        TypeError error = new(fileName,
                            $"Operator {binaryExpr.Operator.Kind} cannot be applied between types '{interner[leftType.NameId]}' and '{interner[rightType.NameId]}'");
                        errors.Add(error);
                        return typeSystem[TypeKind.None];
                    }

                    if (binaryExpr.Operator.Kind == TokenKind.Equal
                        || binaryExpr.Operator.Kind == TokenKind.NotEqual
                        || binaryExpr.Operator.Kind == TokenKind.Greater
                        || binaryExpr.Operator.Kind == TokenKind.Less
                        || binaryExpr.Operator.Kind == TokenKind.GreaterEqual
                        || binaryExpr.Operator.Kind == TokenKind.LessEqual)
                    {
                        return typeSystem[TypeKind.Bool];
                    }

                    return leftType;
                }
            case ExpressionKind.Logical:
                {
                    var logicalExpr = (BoundLogicalExpr)expression;
                    var leftType = InferType(logicalExpr.Left);
                    var rightType = InferType(logicalExpr.Right);

                    if (leftType.Kind == TypeKind.None || rightType.Kind == TypeKind.None)
                    {
                        return typeSystem[TypeKind.None];
                    }

                    if (!(leftType.Kind == TypeKind.Bool && rightType.Kind == TypeKind.Bool))
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; operator {logicalExpr.Operator.Kind} cannot be applied between types {interner[leftType.NameId]} and {interner[rightType.NameId]}"));
                        return typeSystem[TypeKind.None];
                    }

                    return leftType;
                }
            case ExpressionKind.Assignment:
                {
                    var assignmentExpr = (BoundAssignmentExpr)expression;
                    var variableType = assignmentExpr.Symbol.Type;
                    var valueType = InferType(assignmentExpr.Right);

                    if (variableType.Kind == TypeKind.None || valueType.Kind == TypeKind.None)
                    {
                        return typeSystem[TypeKind.None];
                    }

                    if (variableType != valueType)
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; can't assign {interner[valueType.NameId]} to {interner[variableType.NameId]}"));
                        return typeSystem[TypeKind.None];
                    }

                    return variableType;
                }
            case ExpressionKind.Variable:
                {
                    var variableExpr = (BoundVariableExpr)expression;
                    var symbol = variableExpr.Symbol;

                    if (symbol is FunctionSymbol)
                    {
                        string name = interner[variableExpr.Symbol.NameId];
                        errors.Add(new TypeError(fileName,
                            $"Function '{name}' used as a value; did you forget ‘()’?"));
                        return typeSystem[TypeKind.None];
                    }

                    return symbol?.Type ?? typeSystem[TypeKind.None];
                }
            case ExpressionKind.Grouping: return InferType(((BoundGroupingExpr)expression).Expression);
            case ExpressionKind.Call:
                {
                    var callExpr = (BoundCallExpr)expression;
                    return callExpr.Function.Type;
                }
            default: return typeSystem[TypeKind.None];
        }
    }
}

