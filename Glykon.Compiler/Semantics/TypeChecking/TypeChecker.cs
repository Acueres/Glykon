using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Semantics.TypeChecking;

public class TypeChecker(string fileName)
{
    readonly string fileName = fileName;
    readonly List<IGlykonError> errors = [];

    public List<IGlykonError> GetErrors() => errors;

    public TokenKind CheckDeclaredType(BoundExpression expression, TokenKind declaredType)
    {
        TokenKind inferredType = InferType(expression);

        if (declaredType == TokenKind.None) return inferredType;
        if (declaredType != inferredType)
        {
            TypeError error = new(fileName, $"Type mismatch between {declaredType} and {inferredType}");
            errors.Add(error);
            return TokenKind.None;
        }

        return inferredType;
    }

    public void ValidateParameters(IEnumerable<BoundExpression> parameters,  IEnumerable<TokenKind> types)
    {
        foreach (var (parameter, type) in parameters.Zip(types))
        {
            var inferredType = InferType(parameter);
            if (type != inferredType)
            {
                TypeError error = new(fileName, $"Type mismatch between {type} and {inferredType}");
                errors.Add(error);
            }
        }
    }

    public void ValidateCondition(BoundExpression condition)
    {
        TokenKind conditionType = InferType(condition);
        if (conditionType != TokenKind.Bool)
        {
            errors.Add(new TypeError($"Type mismatch: condition must be bool, but got {conditionType}", fileName));
        }
    }

    public void ValidateReturnStatementType(BoundExpression expression, TokenKind expected)
    {
        var actual = InferType(expression);
        if (expected != actual)
        {
            TypeError error = new(fileName, $"Type mismatch. Expected {expected}, got {actual}");
            errors.Add(error);
        }
    }

    public TokenKind InferType(BoundExpression? expression)
    {
        if (expression is null) return TokenKind.None;

        switch (expression.Kind)
        {
            case ExpressionKind.Literal:
                {
                    var literalType = ((BoundLiteralExpr)expression).Token.Kind;
                    return literalType switch
                    {
                        TokenKind.LiteralInt => TokenKind.Int,
                        TokenKind.LiteralReal => TokenKind.Real,
                        TokenKind.LiteralString => TokenKind.String,
                        TokenKind.LiteralTrue => TokenKind.Bool,
                        TokenKind.LiteralFalse => TokenKind.Bool,
                        _ => TokenKind.None,
                    };
                }
            case ExpressionKind.Unary:
                {
                    var unaryExpr = (BoundUnaryExpr)expression;
                    TokenKind operandType = InferType(unaryExpr.Operand);

                    if (operandType == TokenKind.None) return TokenKind.None;

                    if (unaryExpr.Operator.Kind == TokenKind.Not)
                    {
                        if (operandType != TokenKind.Bool)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Kind} cannot be applied to operand type '{operandType}'");
                            errors.Add(error);
                            return TokenKind.None;
                        }

                        return TokenKind.Bool;
                    }

                    if (unaryExpr.Operator.Kind == TokenKind.Minus)
                    {
                        if (operandType != TokenKind.Int && operandType != TokenKind.Real)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Kind} cannot be applied to operand type '{operandType}'");
                            errors.Add(error);
                            return TokenKind.None;
                        }
                    }

                    return operandType;
                }
            case ExpressionKind.Binary:
                {
                    var binaryExpr = (BoundBinaryExpr)expression;
                    TokenKind leftType = InferType(binaryExpr.Left);
                    TokenKind rightType = InferType(binaryExpr.Right);

                    if (leftType == TokenKind.None || rightType == TokenKind.None)
                    {
                        return TokenKind.None;
                    }

                    if (leftType != rightType)
                    {
                        TypeError error = new(fileName,
                            $"Operator {binaryExpr.Operator.Kind} cannot be applied between types '{leftType}' and '{rightType}'");
                        errors.Add(error);
                        return TokenKind.None;
                    }

                    if (binaryExpr.Operator.Kind == TokenKind.Equal
                        || binaryExpr.Operator.Kind == TokenKind.NotEqual
                        || binaryExpr.Operator.Kind == TokenKind.Greater
                        || binaryExpr.Operator.Kind == TokenKind.Less
                        || binaryExpr.Operator.Kind == TokenKind.GreaterEqual
                        || binaryExpr.Operator.Kind == TokenKind.LessEqual)
                    {
                        return TokenKind.Bool;
                    }

                    return leftType;
                }
            case ExpressionKind.Logical:
                {
                    var logicalExpr = (BoundLogicalExpr)expression;
                    TokenKind leftType = InferType(logicalExpr.Left);
                    TokenKind rightType = InferType(logicalExpr.Right);

                    if (leftType == TokenKind.None || rightType == TokenKind.None)
                    {
                        return TokenKind.None;
                    }

                    if (!(leftType == TokenKind.Bool && rightType == TokenKind.Bool))
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; operator {logicalExpr.Operator.Kind} cannot be applied between types {leftType} and {rightType}"));
                        return TokenKind.None;
                    }

                    return leftType;
                }
            case ExpressionKind.Assignment:
                {
                    var assignmentExpr = (BoundAssignmentExpr)expression;
                    TokenKind variableType = assignmentExpr.Symbol.Type;
                    TokenKind valueType = InferType(assignmentExpr.Right);

                    if (variableType == TokenKind.None || valueType == TokenKind.None)
                    {
                        return TokenKind.None;
                    }

                    if (variableType != valueType)
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; can't assign {valueType} to {variableType}"));
                        return TokenKind.None;
                    }

                    return variableType;
                }
            case ExpressionKind.Variable:
                {
                    var variableExpr = (BoundVariableExpr)expression;
                    var symbol = variableExpr.Symbol;

                    if (symbol is FunctionSymbol)
                    {
                        errors.Add(new TypeError(fileName,
                            $"Function '{variableExpr.Name}' used as a value; did you forget ‘()’?"));
                        return TokenKind.None;
                    }

                    return symbol.Type;
                }
            case ExpressionKind.Grouping: return InferType(((BoundGroupingExpr)expression).Expression);
            case ExpressionKind.Call:
                {
                    var callExpr = (BoundCallExpr)expression;
                    return callExpr.Function.Type;
                }
            default: return TokenKind.None;
        }
    }
}

