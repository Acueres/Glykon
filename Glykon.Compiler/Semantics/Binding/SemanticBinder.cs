using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Optimization;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;
using System.Xml.Linq;

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
            case StatementKind.Block:
            {
                Scope scope = symbolTable.BeginScope(ScopeKind.Block);
                var blockStmt = (BlockStmt)stmt;

                var boundStatements = BindStatementsWithDeclarations(blockStmt.Statements);

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
                var variableStmt = (VariableDeclaration)stmt;
                var declaredType = BindTypeAnnotation(variableStmt.DeclaredType);

                var boundExpression = BindExpression(variableStmt.Initializer);

                var symbol = symbolTable.RegisterVariable(variableStmt.Name, variableStmt.Immutable, declaredType);

                return new BoundVariableDeclaration(boundExpression, symbol, declaredType);
            }
            case StatementKind.Constant:
            {
                var constantStmt = (ConstantDeclaration)stmt;
                var declaredType = BindTypeAnnotation(constantStmt.DeclaredType);
                
                var initializer = BindExpression(constantStmt.Initializer);
                
                var symbol = symbolTable.RegisterConstant(constantStmt.Name, declaredType);
                
                return new BoundConstantDeclaration(initializer, symbol);
            }

            case StatementKind.Function:
            {
                var functionStmt = (FunctionDeclaration)stmt;

                TypeSymbol[] paramTypes = [.. functionStmt.Parameters.Select(p => BindTypeAnnotation(p.Type))];
                var returnType = BindTypeAnnotation(functionStmt.ReturnType);

                FunctionSymbol? signature = symbolTable.GetLocalFunction(functionStmt.Name, paramTypes);
                signature ??= symbolTable.RegisterFunction(functionStmt.Name, returnType, paramTypes);

                var scope = symbolTable.BeginScope(signature!);
                signature!.Scope = scope;

                var parameterSymbols = functionStmt.Parameters
                    .Select(p => symbolTable.RegisterParameter(p.Name, BindTypeAnnotation(p.Type))).ToList();

                var boundStatements = BindStatementsWithDeclarations(functionStmt.Body.Statements);
                BoundBlockStmt boundBody = new([.. boundStatements], scope);

                symbolTable.ExitScope();

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
                
                var symbol = symbolTable.GetAllowedSymbol(assignmentExpr.Name);

                BoundExpression right = BindExpression(assignmentExpr.Right);
                
                return new BoundAssignmentExpr(right, symbol);
            }
            case ExpressionKind.Range:
            {
                RangeExpr rangeExpr = (RangeExpr)expression;
                
                var start = BindExpression(rangeExpr.Start);
                var end = BindExpression(rangeExpr.End);
                var step = BindExpression(rangeExpr.Step);
                
                return new BoundRangeExpr(start, end, step, rangeExpr.IsInclusive);
            }
            case ExpressionKind.Variable:
            {
                var variableExpr = (VariableExpr)expression;

                if (!interner.TryGetId(variableExpr.Name, out var id))
                {
                    errors.Add(new BindingError(fileName, $"Unknown identifier: {variableExpr.Name}"));
                    return new BoundInvalidExpr();
                }

                var localVariable = symbolTable.GetLocalVariableSymbol(variableExpr.Name);
                if (localVariable != null)
                {
                    return new BoundVariableExpr(localVariable);
                }

                var symbol = symbolTable.GetSymbol(variableExpr.Name);
                // Captured variables are not allowed
                if (symbol is VariableSymbol)
                {
                    errors.Add(new BindingError(fileName, $"Cannot reference {variableExpr.Name}"));
                    return new BoundInvalidExpr();
                }

                return new BoundVariableExpr(symbol!);
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

                if (callee is not VariableExpr variableExpr)
                {
                    errors.Add(new BindingError(fileName, $"Expression {callee.Kind} is not callable"));
                    return new BoundInvalidExpr();
                }

                int nameId = interner.Intern(variableExpr.Name);
                var overloads = symbolTable.GetFunctionOverloads(variableExpr.Name);
                
                var symbol = symbolTable.GetSymbol(variableExpr.Name);
                if (symbol is not null)
                {
                    errors.Add(new BindingError(fileName, $"{interner[symbol.NameId]} is not callable"));
                    return new BoundInvalidExpr();
                }
                
                if (overloads.Length == 0)
                {
                    errors.Add(new BindingError(fileName, $"{variableExpr.Name} is not a function"));
                    return new BoundInvalidExpr();
                }
                
                return new BoundCallExpr(nameId, overloads, boundArgs);
            }
            default: return new BoundInvalidExpr();
        }
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
        var symbol = symbolTable.GetType(annotation.Name);
        if (symbol is not null) return symbol;
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

        symbolTable.RegisterFunction("println", typeSystem[TypeKind.None], [typeSystem[TypeKind.Int64]]);

        symbolTable.RegisterFunction("println", typeSystem[TypeKind.None], [typeSystem[TypeKind.Float64]]);

        symbolTable.RegisterFunction("println", typeSystem[TypeKind.None], [typeSystem[TypeKind.Bool]]);

        symbolTable.RegisterFunction("println", typeSystem[TypeKind.None], [typeSystem[TypeKind.None]]);
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
