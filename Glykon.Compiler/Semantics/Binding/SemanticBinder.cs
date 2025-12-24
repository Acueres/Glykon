using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics.Binding;

public class SemanticBinder(SyntaxTree syntaxTree, TypeSystem typeSystem, IdentifierInterner interner, LanguageMode mode, string fileName)
{
    readonly SymbolTable symbolTable = new(interner);
    readonly List<IGlykonError> errors = [];

    public (BoundTree, SymbolTable, IGlykonError[]) Bind()
    {
        RegisterPrimitives();
        RegisterStd();

        List<BoundStatement> boundStatements = new(syntaxTree.Length);
        List<Statement> constants = [];
        List<Statement> functions = [];
        List<Statement> statements = [];

        foreach (var node in syntaxTree)
        {
            switch (node)
            {
                case ConstantDeclaration cd:
                    constants.Add(cd);
                    break;

                case FunctionDeclaration fd:
                    functions.Add(fd);
                    break;

                default:
                    if (mode == LanguageMode.Script)
                    {
                        statements.Add(node);
                    }
                    else
                    {
                        ReportTopLevelNotAllowed(node);
                    }
                    break;
            }
        }

        boundStatements.AddRange(constants.Select(BindStatement));
        boundStatements.AddRange(BindStatementsWithDeclarations(functions));
        boundStatements.AddRange(statements.Select(BindStatement));

        BoundTree boundTree = new([.. boundStatements], syntaxTree.FileName);

        return (boundTree, symbolTable, [.. errors]);
    }

    BoundStatement BindStatement(Statement stmt)
    {
        if (stmt is null) return null;

        switch (stmt.Kind)
        {
            case StatementKind.Class:
            {
                var classDecl = (ClassDeclaration)stmt;

                var type = typeSystem.RegisterType(classDecl.Name);
                if (symbolTable.TryGetType(classDecl.Name, out _))
                {
                    errors.Add(new BindingError(fileName, $"Type '{classDecl.Name}' already defined."));
                }
                else
                {
                    symbolTable.RegisterType(type);
                }

                symbolTable.BeginScope(ScopeKind.Type);

                var constants = classDecl.Constants.Select(BindStatement).OfType<BoundConstantDeclaration>()
                    .ToArray();
                var fields = classDecl.Fields.Select(f => BindField(f, type)).ToArray();
                var nested = classDecl.Nested.Select(BindStatement).ToArray();
                var methods = classDecl.Methods.Select(m => BindMethodDeclaration(m, type)).ToArray();

                symbolTable.EndScope();

                type.FinalizeType(methods.Select(m => m.Symbol).ToArray(),
                    fields.Select(f => f.Symbol).ToArray(),
                    constants.Select(c => c.Symbol).ToArray(),
                    nested.OfType<BoundClassDeclaration>().Select(decl => decl.Type).ToArray());

                return new BoundClassDeclaration(type, methods, fields, constants, nested);
            }
            case StatementKind.Block:
            {
                Scope scope = symbolTable.BeginScope(ScopeKind.Block);
                var blockStmt = (BlockStmt)stmt;

                var boundStatements = BindStatementsWithDeclarations(blockStmt.Statements);

                BoundBlockStmt boundBlockStmt = new([.. boundStatements], scope);

                symbolTable.EndScope();
                return boundBlockStmt;
            }
            case StatementKind.If:
            {
                var ifStmt = (IfStmt)stmt;
                var boundThenStmt = BindStatement(ifStmt.ThenStatement);
                var boundElseStmt = BindStatement(ifStmt.ElseStatement);

                BoundExpression boundCondition = BindExpression(ifStmt.Condition);

                BoundIfStmt boundIfStmt = new(boundCondition, boundThenStmt, boundElseStmt);
                return boundIfStmt;
            }
            case StatementKind.While:
            {
                var whileStmt = (WhileStmt)stmt;
                var boundBody = BindStatement(whileStmt.Body);

                BoundExpression boundCondition = BindExpression(whileStmt.Condition);
                BoundWhileStmt boundWhileStmt = new(boundCondition, boundBody);

                return boundWhileStmt;
            }
            case StatementKind.For:
            {
                var forStmt = (ForStmt)stmt;
                
                var iter = BindStatement(forStmt.Iterator);
                var boundRange = BindExpression(forStmt.Range);
                var boundBody = BindStatement(forStmt.Body);
                
                return new BoundForStmt((BoundVariableDeclaration)iter, (BoundRangeExpr)boundRange, boundBody);
            }
            case StatementKind.Variable:
            {
                var variableDecl = (VariableDeclaration)stmt;
                var declaredType = BindTypeAnnotation(variableDecl.DeclaredType);

                var boundExpression = BindExpression(variableDecl.Initializer);

                var symbol = symbolTable.RegisterVariable(variableDecl.Name, variableDecl.Immutable, declaredType);

                return new BoundVariableDeclaration(boundExpression, symbol, declaredType);
            }
            case StatementKind.Constant:
            {
                var constantDecl = (ConstantDeclaration)stmt;
                var declaredType = BindTypeAnnotation(constantDecl.DeclaredType);
                
                var initializer = BindExpression(constantDecl.Initializer);
                
                var symbol = symbolTable.RegisterConstant(constantDecl.Name, declaredType);
                
                return new BoundConstantDeclaration(initializer, symbol);
            }

            case StatementKind.Function:
            {
                var functionDecl = (FunctionDeclaration)stmt;

                TypeSymbol[] paramTypes = [.. functionDecl.Parameters.Select(p => BindTypeAnnotation(p.Type))];
                var returnType = BindTypeAnnotation(functionDecl.ReturnType);

                FunctionSymbol? signature = symbolTable.GetLocalFunction(functionDecl.Name);
                signature ??= symbolTable.RegisterFunction(functionDecl.Name, returnType, paramTypes);

                var scope = symbolTable.BeginScope(signature!);
                
                var parameterSymbols = functionDecl.Parameters
                    .Select(p => symbolTable.RegisterParameter(p.Name, BindTypeAnnotation(p.Type))).ToList();

                var boundStatements = BindStatementsWithDeclarations(functionDecl.Body.Statements);
                BoundBlockStmt boundBody = new([.. boundStatements], scope);

                symbolTable.EndScope();

                return new BoundFunctionDeclaration(signature, [.. parameterSymbols], returnType, boundBody);
            }
            case StatementKind.Return:
            {
                var returnStmt = (ReturnStmt)stmt;
                var currentFunctionSymbol = symbolTable.GetCurrentFunction();
                
                var boundExpression = BindExpression(returnStmt.Expression);
                
                return new BoundReturnStmt(boundExpression, currentFunctionSymbol, returnStmt.Token);
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
            default: return new BoundInvalidStmt();
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
                return new BoundLiteralExpr(literalExpr.Value);
            }
            case ExpressionKind.Unary:
            {
                UnaryExpr unaryExpr = (UnaryExpr)expression;
                BoundExpression operand = BindExpression(unaryExpr.Operand);
                if (operand.Kind == BoundExpressionKind.Invalid) return new BoundInvalidExpr();

                return new BoundUnaryExpr(unaryExpr.Operator, operand);
            }
            case ExpressionKind.Binary:
            {
                BinaryExpr binaryExpr = (BinaryExpr)expression;
                BoundExpression left = BindExpression(binaryExpr.Left);
                BoundExpression right = BindExpression(binaryExpr.Right);

                if (left.Kind == BoundExpressionKind.Invalid || right.Kind == BoundExpressionKind.Invalid)
                {
                   return new BoundInvalidExpr();
                }

                return new BoundBinaryExpr(binaryExpr.Operator, left, right);
            }
            case ExpressionKind.Logical:
            {
                LogicalExpr logicalExpr = (LogicalExpr)expression;
                BoundExpression left = BindExpression(logicalExpr.Left);
                BoundExpression right = BindExpression(logicalExpr.Right);

                if (left.Kind == BoundExpressionKind.Invalid || right.Kind == BoundExpressionKind.Invalid)
                {
                   return new BoundInvalidExpr();
                }

                return new BoundLogicalExpr(logicalExpr.Operator, left, right);
            }
            case ExpressionKind.Assignment:
            {
                AssignmentExpr assignmentExpr = (AssignmentExpr)expression;
                
                var target = BindExpression(assignmentExpr.Target);
                BoundExpression value = BindExpression(assignmentExpr.Right);
                
                if (target is BoundNameExpr variableExpr)
                {
                    return new BoundAssignmentExpr(variableExpr, value);
                }

                if (target is BoundMemberAccessExpr memberAccessExpr)
                {
                    return new BoundAssignmentExpr(memberAccessExpr, value);
                }

                var error = new BindingError(fileName, "Invalid assignment target.");
                errors.Add(error);
                return new BoundInvalidExpr();
            }
            case ExpressionKind.Conversion:
            {
                ConversionExpr conversionExpr = (ConversionExpr)expression;
                
                var boundExpr = BindExpression(conversionExpr.Expression);
                var targetType = BindTypeAnnotation(conversionExpr.TargetType);
                
                if (targetType.IsError) return new BoundInvalidExpr();
                
                return new BoundConversionExpr(boundExpr, targetType);
            }
            case ExpressionKind.MemberAccess:
            {
                MemberAccessExpr memberAccessExpr = (MemberAccessExpr)expression;
                
                var receiver = BindExpression(memberAccessExpr.Receiver);
                int nameId = interner.Intern(memberAccessExpr.Name);
                
                return new BoundMemberAccessExpr(receiver, nameId);
            }
            case ExpressionKind.Range:
            {
                RangeExpr rangeExpr = (RangeExpr)expression;
                
                var start = BindExpression(rangeExpr.Start);
                var end = BindExpression(rangeExpr.End);
                var step = BindExpression(rangeExpr.Step);
                
                return new BoundRangeExpr(start, end, step, rangeExpr.IsInclusive);
            }
            case ExpressionKind.Name:
            {
                var nameExpr = (NameExpr)expression;

                if (!interner.TryGetId(nameExpr.Name, out var id))
                {
                    errors.Add(new BindingError(fileName, $"Unknown identifier: {nameExpr.Name}"));
                    return new BoundInvalidExpr();
                }

                var localVariable = symbolTable.GetLocalVariableSymbol(nameExpr.Name);
                if (localVariable != null)
                {
                    return new BoundNameExpr(localVariable);
                }

                if (symbolTable.TryGetMethod(nameExpr.Name, out var method))
                {
                    return new BoundNameExpr(method!);
                }

                if (symbolTable.TryGetFunction(nameExpr.Name, out var function))
                {
                    return new BoundNameExpr(function!);
                }

                var symbol = symbolTable.GetSymbol(nameExpr.Name);
                // Captured variables are not allowed
                if (symbol is VariableSymbol)
                {
                    errors.Add(new BindingError(fileName, $"Cannot reference {nameExpr.Name}"));
                    return new BoundInvalidExpr();
                }

                return new BoundNameExpr(symbol!);
            }
            case ExpressionKind.Grouping:
            {
                var groupExpr = (GroupingExpr)expression;
                var boundExpression = BindExpression(groupExpr.Expression);
                return new BoundGroupingExpr(boundExpression);
            }
            case ExpressionKind.Call:
            {
                var callExpr = (CallExpr)expression;
                var boundArgs = callExpr.Args.Select(BindExpression).ToArray();

                var callee = callExpr.Callee;
                while (callee is GroupingExpr p) callee = p.Expression;
                
                var boundCallee = BindExpression(callee);
                
                return new BoundCallExpr(boundCallee, boundArgs);
            }
            default: return new BoundInvalidExpr();
        }
    }

    BoundMethodDeclaration BindMethodDeclaration(MethodDeclaration methodDecl, TypeSymbol parentType)
    {
        TypeSymbol[] paramTypes = [.. methodDecl.Parameters.Select(p => BindTypeAnnotation(p.Type))];

        if (!methodDecl.IsStatic && paramTypes.Length > 0)
        {
            paramTypes[0] = parentType;
        }
        
        var returnType = BindTypeAnnotation(methodDecl.ReturnType);
        
        if (symbolTable.TryGetMethod(methodDecl.Name, out var signature))
        {
            var error = new BindingError(fileName, $"Method {methodDecl.Name} already exists.");
            errors.Add(error);
        }
        
        signature ??= symbolTable.RegisterMethod(methodDecl.Name, returnType, parentType, paramTypes, methodDecl.IsStatic);

        var scope = symbolTable.BeginScope(signature!);
        
        ParameterSymbol? thisParameter = null;
        List<ParameterSymbol> parameterSymbols;
        if (methodDecl.IsStatic)
        {
            parameterSymbols = methodDecl.Parameters
                .Select((p, i) => symbolTable.RegisterParameter(p.Name, paramTypes[i])).ToList();
        }
        else
        {
            var parentRef = methodDecl.Parameters.First();
            thisParameter = symbolTable.RegisterParameter(parentRef.Name, parentType);

            parameterSymbols = [thisParameter];
            for (int i = 1; i < methodDecl.Parameters.Length; i++)
            {
                var p =  methodDecl.Parameters[i];
                var parameterSymbol = symbolTable.RegisterParameter(p.Name, paramTypes[i]);
                parameterSymbols.Add(parameterSymbol);
            }
        }

        var boundStatements = BindStatementsWithDeclarations(methodDecl.Body.Statements);
        BoundBlockStmt boundBody = new([.. boundStatements], scope);

        symbolTable.EndScope();

        return new BoundMethodDeclaration(signature!, parentType, [.. parameterSymbols], returnType, boundBody, thisParameter);
    }

    BoundFieldDeclaration BindField(FieldDeclaration fieldDecl, TypeSymbol parentType)
    {
        var declaredType = BindTypeAnnotation(fieldDecl.DeclaredType);

        var boundExpression = BindExpression(fieldDecl.Initializer);

        var symbol = symbolTable.RegisterField(fieldDecl.Name, declaredType, parentType);

        return new BoundFieldDeclaration(boundExpression, symbol);
    }

    List<BoundStatement> BindStatementsWithDeclarations(List<Statement> statements)
    {
        List<BoundStatement> boundStatements = new(statements.Count);

        var declarations = statements.Where(s => s.Kind == StatementKind.Function)
            .Cast<FunctionDeclaration>()
            .ToList();
        var nonDeclarations = statements.Where(s => s.Kind != StatementKind.Function)
            .ToList();

        // Predeclaring function definitions
        foreach (var f in declarations)
        {
            var returnType = BindTypeAnnotation(f.ReturnType);
            TypeSymbol[] parameters = [.. f.Parameters.Select(p => BindTypeAnnotation(p.Type))];
            symbolTable.RegisterFunction(f.Name, returnType, parameters);
        }

        boundStatements.AddRange(nonDeclarations.Select(BindStatement));
        boundStatements.AddRange(declarations.Select(BindStatement));

        return boundStatements;
    }

    TypeSymbol BindTypeAnnotation(TypeAnnotation annotation)
    {
        if (symbolTable.TryGetType(annotation.Name, out var typeSymbol)) return typeSymbol!;
        errors.Add(new BindingError(fileName, $"Unknown type: {annotation.Name}"));
        return typeSystem[TypeKind.Error];
    }

    void RegisterPrimitives()
    {
        foreach (var symbol in typeSystem.GetPrimitives())
        {
            symbolTable.RegisterType(symbol);
        }
    }

    void RegisterStd()
    {
        symbolTable.RegisterFunction("println", typeSystem[TypeKind.None], [typeSystem[TypeKind.String]]);
    }

    private void ReportTopLevelNotAllowed(Statement stmt)
    {
        var msg = stmt switch
        {
            VariableDeclaration => "Top-level variables are not allowed. Use 'const' or move it inside a function.",
            ExpressionStmt => "Top-level expressions are not allowed. Move the code inside a function (e.g., 'def main').",
            ReturnStmt => "Top-level 'return' is not allowed.",
            JumpStmt => "Top-level loop/control statements are not allowed.",
            _ => "Top-level statements are not allowed in application mode."
        };

        errors.Add(new BindingError(fileName, msg));
    }
}
