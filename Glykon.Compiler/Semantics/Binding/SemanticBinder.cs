using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.TypeChecking;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding;

public class SemanticBinder(SyntaxTree syntaxTree, IdentifierInterner interner, string fileName)
{
    readonly SyntaxTree syntaxTree = syntaxTree;
    readonly SymbolTable symbolTable = new(interner);
    readonly TypeChecker typeChecker = new(syntaxTree.FileName);
    readonly List<IGlykonError> errors = [];
    readonly string fileName = fileName;

    public List<IGlykonError> GetErrors()
    {
        var checkerErrors = typeChecker.GetErrors();
        return [.. errors, .. checkerErrors];
    }

    public (BoundTree, SymbolTable) Bind()
    {
        RegisterStd();

        List<BoundStatement> boundStatements = new(syntaxTree.Length);

        foreach (var stmt in syntaxTree)
        {
            var boundStatement = BindStatement(stmt);
            boundStatements.Add(boundStatement);
        }

        BoundTree boundTree = new([..boundStatements], syntaxTree.FileName);

        return (boundTree, symbolTable);
    }

    BoundStatement BindStatement(Statement stmt)
    {
        if (stmt is null) return null;

        switch (stmt.Kind)
        {
            case StatementKind.Block:
                {
                    Scope scope = symbolTable.BeginScope(ScopeKind.Block);
                    var blockStmt = (BlockStmt)stmt;

                    List<BoundStatement> boundStatements = new(blockStmt.Statements.Count);
                    foreach (var statement in blockStmt.Statements)
                    {
                        var boundStatement = BindStatement(statement);
                        boundStatements.Add(boundStatement);
                    }

                    BoundBlockStmt boundBlockStmt = new([.. boundStatements], scope);

                    symbolTable.ExitScope();
                    return boundBlockStmt;
                }
            case StatementKind.If:
                {
                    var ifStmt = (IfStmt)stmt;
                    var boundThenStmt = BindStatement(ifStmt.ThenStatement);
                    var boundElseStmt = BindStatement(ifStmt.ElseStatement);

                    BoundExpression boundCondition = BindExpression(ifStmt.Condition);
                    typeChecker.ValidateCondition(boundCondition);

                    BoundIfStmt boundIfStmt = new(boundCondition, boundThenStmt, boundElseStmt);
                    return boundIfStmt;
                }
            case StatementKind.While:
                {
                    var whileStmt = (WhileStmt)stmt;
                    var boundStatement = BindStatement(whileStmt.Statement);

                    BoundExpression boundCondition = BindExpression(whileStmt.Condition);
                    typeChecker.ValidateCondition(boundCondition);

                    BoundWhileStmt boundWhileStmt = new(boundCondition, boundStatement);

                    return boundWhileStmt;
                }
            case StatementKind.Variable:
                {
                    var variableStmt = (VariableDeclaration)stmt;
                    BoundExpression boundExpression = BindExpression(variableStmt.Expression);
                    var variableType = typeChecker.CheckDeclaredType(boundExpression, variableStmt.DeclaredType);

                    symbolTable.RegisterVariable(variableStmt.Name, variableType);

                    return new BoundVariableDeclaration(boundExpression, variableStmt.Name, variableType);
                }
            case StatementKind.Constant:
                {
                    var constantStmt = (ConstantDeclaration)stmt;

                    symbolTable.RegisterConstant(constantStmt.Name, constantStmt.Token, constantStmt.DeclaredType);

                    BoundExpression boundExpression = BindExpression(constantStmt.Expression);
                    var constantType = typeChecker.CheckDeclaredType(boundExpression, constantStmt.DeclaredType);
                    return new BoundConstantDeclaration(boundExpression, constantStmt.Name, constantStmt.Token, constantType);
                }

            case StatementKind.Function:
                {
                    var functionStmt = (FunctionDeclaration)stmt;

                    var signature = symbolTable.RegisterFunction(functionStmt.Name, functionStmt.ReturnType, [.. functionStmt.Parameters.Select(p => p.Type)]);
                    var scope = symbolTable.BeginScope(signature);

                    List<ParameterSymbol> parameterSymbols = new(functionStmt.Parameters.Count);
                    foreach (var parameter in functionStmt.Parameters)
                    {
                        var parameterSymbol = symbolTable.RegisterParameter(parameter.Name, parameter.Type);
                        parameterSymbols.Add(parameterSymbol);
                    }

                    List<BoundStatement> boundStatements = new(functionStmt.Body.Statements.Count);
                    var functionDeclarations = functionStmt.Body.Statements.Where(stmt =>  stmt.Kind == StatementKind.Function);
                    var statements = functionStmt.Body.Statements.Where(stmt => stmt.Kind != StatementKind.Function);
                    foreach (var statement in functionDeclarations)
                    {
                        var boundStatement = BindStatement(statement);
                        boundStatements.Add(boundStatement);
                    }

                    foreach (var statement in statements)
                    {
                        var boundStatement = BindStatement(statement);
                        boundStatements.Add(boundStatement);
                    }

                    BoundBlockStmt boundBody = new([.. boundStatements], scope);

                    symbolTable.ExitScope();

                    return new BoundFunctionDeclaration(functionStmt.Name, signature, [.. parameterSymbols], functionStmt.ReturnType, boundBody);
                }
            case StatementKind.Return:
                {
                    var returnStmt = (ReturnStmt)stmt;

                    var currentFunctionSymbol = symbolTable.GetCurrentContainingFunction();

                    var boundExpression = BindExpression(returnStmt.Expression);

                    typeChecker.ValidateReturnStatementType(boundExpression, currentFunctionSymbol.Type);

                    return new BoundReturnStmt(boundExpression);
                }
            case StatementKind.Expression:
                {
                    var expression = (ExpressionStmt)stmt;
                    var boundExpression = BindExpression(expression.Expression);
                    return new BoundExpressionStmt(boundExpression);
                }
            case StatementKind.Jump:
                {
                    var jumpStmt = (JumpStmt)stmt;
                    return new BoundJumpStmt(jumpStmt.Token);
                }
            default: return null;
        }
    }

    BoundExpression BindExpression(Expression expression)
    {
        if (expression is null) return null;

        switch (expression.Kind)
        {
            case ExpressionKind.Literal:
                {
                    LiteralExpr literalExpr = (LiteralExpr)expression;
                    return new BoundLiteralExpr(literalExpr.Token);
                }
            case ExpressionKind.Unary:
                {
                    UnaryExpr unaryExpr = (UnaryExpr)expression;
                    BoundExpression operand = BindExpression(unaryExpr.Operand);
                    return new BoundUnaryExpr(unaryExpr.Operator, operand);
                }
            case ExpressionKind.Binary:
                {
                    BinaryExpr binaryExpr = (BinaryExpr)expression;
                    BoundExpression left = BindExpression(binaryExpr.Left);
                    BoundExpression right = BindExpression(binaryExpr.Right);
                    return new BoundBinaryExpr(binaryExpr.Operator, left, right);
                }
            case ExpressionKind.Logical:
                {
                    LogicalExpr logicalExpr = (LogicalExpr)expression;
                    BoundExpression left = BindExpression(logicalExpr.Left);
                    BoundExpression right = BindExpression(logicalExpr.Right);
                    return new BoundLogicalExpr(logicalExpr.Operator, left, right);
                }
            case ExpressionKind.Assignment:
                {
                    AssignmentExpr assignmentExpr = (AssignmentExpr)expression;
                    BoundExpression right = BindExpression(assignmentExpr.Right);
                    Symbol symbol = symbolTable.GetSymbol(assignmentExpr.Name);
                    return new BoundAssignmentExpr(assignmentExpr.Name, right, symbol);
                }
            case ExpressionKind.Variable:
                {
                    var variableExpr = (VariableExpr)expression;
                    var symbol = symbolTable.GetSymbol(variableExpr.Name);
                    return new BoundVariableExpr(variableExpr.Name, symbol);
                }
            case ExpressionKind.Grouping:
                {
                    var groupExpr = (GroupingExpr)expression;
                    BoundExpression boundExpression = BindExpression(groupExpr.Expression);
                    return new BoundGroupingExpr(boundExpression);
                }
            case ExpressionKind.Call:
                {
                    var callExpr = (CallExpr)expression;

                    var boundArgs = callExpr.Args.Select(BindExpression).ToArray();
                    var argTypes = boundArgs.Select(typeChecker.InferType).ToArray();

                    var callee = callExpr.Callee;
                    while (callee is GroupingExpr p) callee = p.Expression;

                    if (callee is not VariableExpr id)
                    {
                        errors.Add(new TypeError(fileName, $"Expression {callee.Kind} is not callable"));
                        return null;
                    }

                    if (symbolTable.IsFunction(id.Name))
                    {
                        var fn = symbolTable.GetFunction(id.Name, argTypes);
                        if (fn != null)
                        {
                            return new BoundCallExpr(fn, boundArgs);
                        }
                        else
                        {
                            errors.Add(new TypeError(fileName, $"{id.Name} is not a function"));
                            return null;
                        }
                    }

                    if (symbolTable.GetSymbol(id.Name) != null)
                    {
                        errors.Add(new TypeError(fileName, $"{id.Name} is not callable"));
                        return null;
                    }

                    errors.Add(new TypeError(fileName, $"Unknown identifier: {id.Name}"));
                    return null;
                }
            default: return null;
        }
    }

    void RegisterStd()
    {
        symbolTable.RegisterFunction("println", TokenKind.None, [TokenKind.String]);

        symbolTable.RegisterFunction("println", TokenKind.None, [TokenKind.Int]);

        symbolTable.RegisterFunction("println", TokenKind.None, [TokenKind.Real]);

        symbolTable.RegisterFunction("println", TokenKind.None, [TokenKind.Bool]);
    }
}
