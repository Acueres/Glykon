using TythonCompiler.Diagnostics.Errors;
using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Syntax.Statements;
using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis;

class TypeChecker(SymbolTable symbolTable, string fileName)
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
        }
    }

    public List<ITythonError> GetErrors() => errors;

    void CheckFunctionDeclaration(FunctionStmt fStmt)
    {
        symbolTable.EnterScope(fStmt.ScopeIndex);

        foreach (var statement in fStmt.Body)
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
        Analyze(stmt.Statement);
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

    // TODO: Complete function return type checking 
    void CheckReturnStatement(ReturnStmt returnStmt)
    {

    }

    TokenType InferType(IExpression expression)
    {
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

                    return InferType(unaryExpr.Expression);
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
                    TokenType variableType = symbolTable.GetType(assignmentExpr.Name);
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
                    return symbolTable.GetType(variableExpr.Name);
                }
            case ExpressionType.Grouping: return InferType(((GroupingExpr)expression).Expression);
            case ExpressionType.Call:
                {
                    // TODO: Add call type checking
                    CallExpr callExpr = (CallExpr)expression;
                    return TokenType.None;
                }
            default: return TokenType.None;
        }
    }
}

