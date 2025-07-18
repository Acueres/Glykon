using TythonCompiler.Diagnostics.Errors;
using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Syntax.Statements;
using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis;

public class TypeChecker(SymbolTable symbolTable, string fileName)
{
    readonly string fileName = fileName;

    readonly SymbolTable symbolTable = symbolTable;
    readonly List<ITythonError> errors = [];

    public void Analyze(IStatement statement)
    {
        switch (statement.Type)
        {
            case StatementType.If: CheckIfStatement((IfStmt)statement); break;
            case StatementType.While: CheckWhileStatement((WhileStmt)statement); break;
            case StatementType.Variable: CheckVariableDeclaration((VariableStmt)statement); break;
            case StatementType.Constant: CheckConstantDeclaration((ConstantStmt)statement); break;
            case StatementType.Function: CheckFunctionDeclaration((FunctionStmt)statement); break;
            case StatementType.Block: CheckBlockStatement((BlockStmt)statement); break;
            case StatementType.Return: CheckReturnStatement((ReturnStmt)statement); break;
            case StatementType.Expression: CheckExpressionStatement((ExpressionStmt)statement); break;
        }
    }

    public List<ITythonError> GetErrors() => errors;

    void CheckFunctionDeclaration(FunctionStmt fStmt)
    {
        BlockStmt functionBody = fStmt.Body;
        symbolTable.EnterScope(functionBody.ScopeIndex);

        foreach (var statement in fStmt.Body.Statements)
        {
            Analyze(statement);
        }

        symbolTable.ExitScope();
    }

    void CheckBlockStatement(BlockStmt blockStmt)
    {
        symbolTable.EnterScope(blockStmt.ScopeIndex);

        foreach (var statement in blockStmt.Statements)
        {
            Analyze(statement);
        }

        symbolTable.ExitScope();
    }

    void CheckIfStatement(IfStmt stmt)
    {
        ValidateCondition(stmt.Expression);
        Analyze(stmt.ThenStatement);
        if (stmt.ElseStatement is not null) Analyze(stmt.ElseStatement);
    }

    void CheckWhileStatement(WhileStmt stmt)
    {
        ValidateCondition(stmt.Expression);
        Analyze(stmt.Statement);
    }

    private void ValidateCondition(IExpression condition)
    {
        TokenType conditionType = InferType(condition);
        if (conditionType != TokenType.Bool)
        {
            errors.Add(new TypeError($"Type mismatch: condition must be bool, but got {conditionType}", fileName));
        }
    }

    void CheckVariableDeclaration(VariableStmt variableStmt)
    {
        TokenType inferredType = InferType(variableStmt.Expression);
        if (variableStmt.VariableType == TokenType.None)
        {
            variableStmt.VariableType = inferredType;
            symbolTable.UpdateType(variableStmt.Name, inferredType);
        }
        else if (variableStmt.VariableType != inferredType)
        {
            TypeError error = new(fileName, "Type mismatch");
            errors.Add(error);
        }
    }

    void CheckConstantDeclaration(ConstantStmt constantStmt)
    {
        TokenType inferredType = InferType(constantStmt.Expression);
        if (constantStmt.ConstantType != inferredType)
        {
            TypeError error = new(fileName, "Type mismatch");
            errors.Add(error);
        }
    }


    void CheckReturnStatement(ReturnStmt returnStmt)
    {
        var currentFunctionSymbol = symbolTable.GetCurrentContainingFunction();

        var expected = currentFunctionSymbol.Type;
        if (returnStmt.Expression is null && expected != TokenType.None)
        {
            TypeError error = new(fileName, $"Type mismatch. Expected {expected}, got None");
            errors.Add(error);
            return;
        }

        var actual = InferType(returnStmt.Expression);
        if (expected != actual)
        {
            TypeError error = new(fileName, $"Type mismatch. Expected {expected}, got {actual}");
            errors.Add(error);
        }
    }

    void CheckExpressionStatement(ExpressionStmt expressionStmt)
    {
        InferType(expressionStmt.Expression);
    }

    TokenType InferType(IExpression? expression)
    {
        if (expression is null) return TokenType.None;

        switch (expression.Type)
        {
            case ExpressionType.Literal:
                {
                    var literalType = ((LiteralExpr)expression).Token.Type;
                    return literalType switch
                    {
                        TokenType.LiteralInt => TokenType.Int,
                        TokenType.LiteralReal => TokenType.Real,
                        TokenType.LiteralString => TokenType.String,
                        TokenType.LiteralTrue => TokenType.Bool,
                        TokenType.LiteralFalse => TokenType.Bool,
                        _ => TokenType.None,
                    };
                }
            case ExpressionType.Unary:
                {
                    UnaryExpr unaryExpr = (UnaryExpr)expression;
                    TokenType operandType = InferType(unaryExpr.Expression);

                    if (operandType == TokenType.None) return TokenType.None;

                    if (unaryExpr.Operator.Type == TokenType.Not)
                    {
                        if (operandType != TokenType.Bool)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Type} cannot be applied to operand type '{operandType}'");
                            errors.Add(error);
                            return TokenType.None;
                        }

                        return TokenType.Bool;
                    }

                    if (unaryExpr.Operator.Type == TokenType.Minus)
                    {
                        if (operandType != TokenType.Int && operandType != TokenType.Real)
                        {
                            TypeError error = new(fileName,
                            $"Operator {unaryExpr.Operator.Type} cannot be applied to operand type '{operandType}'");
                            errors.Add(error);
                            return TokenType.None;
                        }
                    }

                    return operandType;
                }
            case ExpressionType.Binary:
                {
                    BinaryExpr binaryExpr = (BinaryExpr)expression;
                    TokenType leftType = InferType(binaryExpr.Left);
                    TokenType rightType = InferType(binaryExpr.Right);

                    if (leftType == TokenType.None || rightType == TokenType.None)
                    {
                        return TokenType.None;
                    }

                    if (leftType != rightType)
                    {
                        TypeError error = new(fileName,
                            $"Operator {binaryExpr.Operator.Type} cannot be applied between types '{leftType}' and '{rightType}'");
                        errors.Add(error);
                        return TokenType.None;
                    }

                    if (binaryExpr.Operator.Type == TokenType.Equal
                        || binaryExpr.Operator.Type == TokenType.NotEqual
                        || binaryExpr.Operator.Type == TokenType.Greater
                        || binaryExpr.Operator.Type == TokenType.Less
                        || binaryExpr.Operator.Type == TokenType.GreaterEqual
                        || binaryExpr.Operator.Type == TokenType.LessEqual)
                    {
                        return TokenType.Bool;
                    }

                    return leftType;
                }
            case ExpressionType.Logical:
                {
                    LogicalExpr logicalExpr = (LogicalExpr)expression;
                    TokenType leftType = InferType(logicalExpr.Left);
                    TokenType rightType = InferType(logicalExpr.Right);

                    if (leftType == TokenType.None || rightType == TokenType.None)
                    {
                        return TokenType.None;
                    }

                    if (!(leftType == TokenType.Bool && rightType == TokenType.Bool))
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; operator {logicalExpr.Operator.Type} cannot be applied between types {leftType} and {rightType}"));
                        return TokenType.None;
                    }

                    return leftType;
                }
            case ExpressionType.Assignment:
                {
                    AssignmentExpr assignmentExpr = (AssignmentExpr)expression;
                    TokenType variableType = symbolTable.GetSymbol(assignmentExpr.Name).Type;
                    TokenType valueType = InferType(assignmentExpr.Right);

                    if (variableType == TokenType.None || valueType == TokenType.None)
                    {
                        return TokenType.None;
                    }

                    if (variableType != valueType)
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; can't assign {valueType} to {variableType}"));
                        return TokenType.None;
                    }

                    return variableType;
                }
            case ExpressionType.Variable:
                {
                    var variableExpr = (VariableExpr)expression;

                    if (symbolTable.IsFunction(variableExpr.Name))
                    {
                        errors.Add(new TypeError(fileName,
                            $"Function '{variableExpr.Name}' used as a value; did you forget ‘()’?"));
                        return TokenType.None;
                    }

                    var symbol = symbolTable.GetSymbol(variableExpr.Name);
                    return symbol.Type;
                }
            case ExpressionType.Grouping: return InferType(((GroupingExpr)expression).Expression);
            case ExpressionType.Call:
                {
                    CallExpr callExpr = (CallExpr)expression;

                    if (callExpr.Callee is VariableExpr callee
                    && !symbolTable.IsFunction(callee.Name))
                    {
                        errors.Add(new TypeError(fileName, "Target of call is not a function"));
                        return TokenType.None;
                    }

                    callee = (VariableExpr)callExpr.Callee;

                    var argTypes = callExpr.Args.Select(InferType).ToArray();
                    if (argTypes.Any(t => t == TokenType.None)) return TokenType.None;

                    var function = symbolTable.GetFunction(callee.Name, argTypes);
                    if (function is null)
                    {
                        errors.Add(new TypeError(fileName,
                            $"No overload of '{callee.Name}' matches ({string.Join(", ", argTypes)})"));
                        return TokenType.None;
                    }

                    return function.Type;
                }
            default: return TokenType.None;
        }
    }
}

