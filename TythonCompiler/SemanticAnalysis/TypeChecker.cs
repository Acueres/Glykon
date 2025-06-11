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

    public void CheckTypes(IStatement statement)
    {
        switch (statement.Type)
        {
            case StatementType.If:
            case StatementType.While: CheckConditionType(statement); break;
            case StatementType.Variable: CheckAssignedType(statement); break;
            case StatementType.Constant: CheckConstantType(statement); break;
            case StatementType.Block: CheckBlockStatement((BlockStmt)statement); break;
            case StatementType.Function: CheckFunction((FunctionStmt)statement); break;
        }
    }

    public List<ITythonError> GetErrors() => errors;

    void CheckFunction(FunctionStmt fStmt)
    {
        symbolTable.EnterScope(fStmt.ScopeIndex);

        foreach (var statement in fStmt.Body)
        {
            CheckTypes(statement);
        }

        symbolTable.ExitScope();
    }

    void CheckBlockStatement(BlockStmt blockStmt)
    {
        symbolTable.EnterScope(blockStmt.ScopeIndex);

        foreach (var statement in blockStmt.Statements)
        {
            CheckTypes(statement);
        }

        symbolTable.ExitScope();
    }

    void CheckConditionType(IStatement stmt)
    {
        TokenType conditionType = InfereType(stmt.Expression);
        if (!(conditionType == TokenType.Bool))
        {
            errors.Add(new TypeError($"Type mismatch: expected bool, got {conditionType}", fileName));
        }
    }

    void CheckAssignedType(IStatement stmt)
    {
        VariableStmt variableStmt = (VariableStmt)stmt;
        TokenType inferredType = InfereType(variableStmt.Expression);
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

    void CheckConstantType(IStatement stmt)
    {
        ConstantStmt constantStmt = (ConstantStmt)stmt;
        TokenType inferredType = InfereType(constantStmt.Expression);
        if (constantStmt.ConstantType != inferredType)
        {
            TypeError error = new(fileName, "Type mismatch");
            errors.Add(error);
        }
    }

    TokenType InfereType(IExpression expression)
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
                    return InfereType(unaryExpr.Expression);
                }
            case ExpressionType.Binary:
                {
                    BinaryExpr binaryExpr = (BinaryExpr)expression;
                    TokenType leftType = InfereType(binaryExpr.Left);
                    TokenType rightType = InfereType(binaryExpr.Right);
                    if (leftType != rightType)
                    {
                        TypeError error = new(fileName,
                            $"Operator {binaryExpr.Operator.Type} cannot be applied between types '{leftType}' and '{rightType}'");
                        errors.Add(error);
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
                    TokenType leftType = InfereType(logicalExpr.Left);
                    TokenType rightType = InfereType(logicalExpr.Right);

                    if (!(leftType == TokenType.Bool && rightType == TokenType.Bool))
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; operator {logicalExpr.Operator.Type} cannot be applied between types {leftType} and {rightType}"));
                    }

                    return leftType;
                }
            case ExpressionType.Assignment:
                {
                    AssignmentExpr assignmentExpr = (AssignmentExpr)expression;
                    TokenType variableType = symbolTable.GetType(assignmentExpr.Name);
                    TokenType valueType = InfereType(assignmentExpr.Right);

                    if (variableType != valueType)
                    {
                        errors.Add(new TypeError(fileName, $"Type mismatch; can't assign {valueType} to {variableType}"));
                    }

                    return variableType;
                }
            case ExpressionType.Variable:
                {
                    var variableExpr = (VariableExpr)expression;
                    return symbolTable.GetType(variableExpr.Name);
                }
            case ExpressionType.Grouping: return InfereType(((GroupingExpr)expression).Expression);
            default: return TokenType.None;
        }
    }
}

