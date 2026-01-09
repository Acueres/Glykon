using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR;

public class IRBuilder(
    BoundTree boundTree,
    TypeSystem typeSystem,
    IdentifierInterner interner,
    string fileName)
{
    private readonly List<IGlykonError> errors = [];
    private readonly IRExpression invalidExpr = new IRInvalidExpr(typeSystem[TypeKind.Error]);
    private readonly Dictionary<FieldSymbol, IRExpression> fieldDefaultValues = [];
    private readonly int constructorNameId = interner.Intern("init");

    public (IRTree, IGlykonError[]) Build()
    {
        PrecomputeFieldDefaultValues();
        
        List<IRStatement> irStatements = new(boundTree.Length);
        irStatements.AddRange(boundTree.Select(BuildStatement));

        IRTree irTree = new([..irStatements], fileName);

        return (irTree, [..errors]);
    }

    private IRStatement BuildStatement(BoundStatement stmt)
    {
        switch (stmt.Kind)
        {
            case BoundStatementKind.Type:
            {
                var typeDeclaration = (BoundTypeDeclaration)stmt;

                var constants = typeDeclaration.Constants.Select(BuildStatement).OfType<IRConstantDeclaration>().ToArray();
                var fields = typeDeclaration.Fields.Select(f => new IRFieldDeclaration(f.Symbol)).ToArray();
                var nested = typeDeclaration.Nested.Select(BuildStatement).OfType<IRTypeDeclaration>().ToArray();
                var methods = typeDeclaration.Methods.Select(BuildMethodDeclaration).ToArray();

                return new IRTypeDeclaration(typeDeclaration.Type, methods, fields, constants, nested);
            }
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
            case BoundStatementKind.For:
            {
                var forStmt = (BoundForStmt)stmt;

                var iter = BuildStatement(forStmt.Iterator);
                var range = BuildExpression(forStmt.Range);
                var body = BuildStatement(forStmt.Body);

                return new IRForStmt((IRVariableDeclaration)iter, (IRRangeExpr)range, body);
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

                if (initializer.Kind == IRExpressionKind.Invalid)
                    return new IRConstantDeclaration(initializer, constantStmt.Symbol);
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

                if (returnStmt.ContainerSymbol is null) return new IRInvalidStmt();

                var returnType = returnStmt.ContainerSymbol.Type;
                var value = returnStmt.Value is null ? null : BuildExpression(returnStmt.Value);

                if (value is not null)
                {
                    if (value.Kind == IRExpressionKind.Invalid) return new IRInvalidStmt();
                    if (TypeSystem.CanImplicitlyConvert(value.Type, returnType))
                    {
                        value = new IRConversionExpr(value, returnType);
                    }
                }

                CheckReturnStatementType(value, returnType);
                return new IRReturnStmt(value, returnStmt.Token);
            }
            case BoundStatementKind.Expression:
            {
                var expression = (BoundExpressionStmt)stmt;
                var irExpression = BuildExpression(expression.Expression);

                if (irExpression.Type.Kind == TypeKind.None) return new IRExpressionStmt(irExpression);

                if (irExpression.Type.Kind != TypeKind.Error)
                {
                    errors.Add(new TypeError(fileName, "Non-void expression used as a statement."));
                }

                return new IRInvalidStmt();

            }
            case BoundStatementKind.Jump:
            {
                var jumpStmt = (BoundJumpStmt)stmt;
                return new IRJumpStmt(jumpStmt.Token);
            }
            default: return new IRInvalidStmt();
        }
    }

    private void PrecomputeFieldDefaultValues()
    {
        foreach (var stmt in boundTree)
        {
            PrecomputeInStatement(stmt);
        }
    }

    private void PrecomputeInStatement(BoundStatement stmt)
    {
        switch (stmt.Kind)
        {
            case BoundStatementKind.Type:
            {
                var c = (BoundTypeDeclaration)stmt;
                
                foreach (var n in c.Nested)
                    PrecomputeInStatement(n);

                foreach (var f in c.Fields)
                    PrecomputeFieldDefault(f);

                break;
            }
            
            case BoundStatementKind.Block:
            {
                var b = (BoundBlockStmt)stmt;
                foreach (var s in b.Statements)
                    PrecomputeInStatement(s);
                break;
            }

            case BoundStatementKind.If:
            {
                var i = (BoundIfStmt)stmt;
                PrecomputeInStatement(i.ThenStatement);
                if (i.ElseStatement is not null)
                    PrecomputeInStatement(i.ElseStatement);
                break;
            }

            case BoundStatementKind.While:
            {
                var w = (BoundWhileStmt)stmt;
                PrecomputeInStatement(w.Body);
                break;
            }

            case BoundStatementKind.For:
            {
                var f = (BoundForStmt)stmt;
                PrecomputeInStatement(f.Iterator);
                PrecomputeInStatement(f.Body);
                break;
            }
        }
    }

    private void PrecomputeFieldDefault(BoundFieldDeclaration fieldDec)
    {
        if (fieldDec.Initializer is null)
        {
            return;
        }

        var ir = BuildExpression(fieldDec.Initializer);
        if (ir.Kind == IRExpressionKind.Invalid)
        {
            return;
        }

        ir = CoerceOrError(ir, fieldDec.Symbol.Type);
        if (ir.Kind == IRExpressionKind.Invalid)
            return;
        
        fieldDefaultValues[fieldDec.Symbol] = ir;
    }

    private IRMethodDeclaration BuildMethodDeclaration(BoundMethodDeclaration methodDec)
    {
        var irStatements = methodDec.Body.Statements.Select(BuildStatement).ToArray();
        IRBlockStmt irBody = new([.. irStatements], methodDec.Body.Scope);

        return new IRMethodDeclaration(methodDec.Symbol, methodDec.ParentType, methodDec.Parameters,
            methodDec.ReturnType, irBody, methodDec.IsStatic);
    }

    private IRExpression BuildExpression(BoundExpression expression)
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
                    return invalidExpr;
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
                    return invalidExpr;
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

                var target = BuildExpression(assignmentExpr.Target);
                var value = BuildExpression(assignmentExpr.Value);

                if (target.Kind == IRExpressionKind.Invalid || value.Kind == IRExpressionKind.Invalid)
                {
                    return invalidExpr;
                }

                // Variable assignment
                if (target is IRNameExpr v)
                {
                    if (v.Symbol is not VariableSymbol varSym)
                    {
                        return v.Symbol switch
                        {
                            ConstantSymbol => NotAssignable("Can't assign to a constant."),
                            FunctionSymbol => NotAssignable("Can't assign to a function."),
                            MethodSymbol => NotAssignable("Can't assign to a method."),
                            ParameterSymbol => NotAssignable("Can't assign to a parameter."),
                            _ => NotAssignable("Invalid assignment target.")
                        };
                    }

                    if (varSym.Immutable)
                    {
                        return NotAssignable("Can't assign to an immutable variable.");
                    }

                    value = CoerceOrError(value, varSym.Type);
                    if (value.Kind == IRExpressionKind.Invalid)
                    {
                        return invalidExpr;
                    }

                    return new IRAssignmentExpr(value, varSym, typeSystem[TypeKind.None]);
                }

                // Field assignment
                if (target is IRMemberAccessExpr ma)
                {
                    if (ma.MemberSymbol is not FieldSymbol field)
                        return NotAssignable("Can only assign to fields.");

                    value = CoerceOrError(value, field.Type);
                    if (value.Kind == IRExpressionKind.Invalid)
                    {
                        return invalidExpr;
                    }

                    return new IRFieldAssignmentExpr(ma.Receiver, field, value, typeSystem[TypeKind.None]);
                }

                return NotAssignable("Invalid assignment expression.");

                IRExpression NotAssignable(string msg)
                {
                    errors.Add(new TypeError(fileName, msg));
                    return invalidExpr;
                }
            }
            case BoundExpressionKind.Range:
            {
                var rangeExpr = (BoundRangeExpr)expression;

                var start = BuildExpression(rangeExpr.Start);
                var end = BuildExpression(rangeExpr.End);
                IRExpression? step = null;
                if (rangeExpr.Step is not null)
                {
                    step = BuildExpression(rangeExpr.Step);
                }

                if (start.Type.Kind != TypeKind.Int64 || end.Type.Kind != TypeKind.Int64
                                                      || (step is not null && step.Type.Kind != TypeKind.Int64))
                {
                    var error = new TypeError(fileName, "Range expression must be of type \"int\".");
                    errors.Add(error);
                    return invalidExpr;
                }

                return new IRRangeExpr(start, end, step, rangeExpr.IsInclusive);
            }
            case BoundExpressionKind.Name:
            {
                var variableExpr = (BoundNameExpr)expression;
                var symbol = variableExpr.Symbol;
                return new IRNameExpr(symbol);
            }
            case BoundExpressionKind.MemberAccess:
            {
                var memberExpr = (BoundMemberAccessExpr)expression;

                var receiver = BuildExpression(memberExpr.Receiver);
                if (receiver.Kind == IRExpressionKind.Invalid)
                {
                    return invalidExpr;
                }

                var symbol = receiver.Type.Find(memberExpr.NameId);
                if (symbol is null)
                {
                    var error = new TypeError(fileName,
                        $"Unknown symbol {interner[memberExpr.NameId]} on type {interner[receiver.Type.NameId]}");
                    errors.Add(error);
                    return invalidExpr;
                }

                return new IRMemberAccessExpr(receiver, symbol);
            }
            case BoundExpressionKind.Grouping:
            {
                var groupExpr = (BoundGroupingExpr)expression;
                var irExpression = BuildExpression(groupExpr.Expression);
                return new IRGroupingExpr(irExpression);
            }
            case BoundExpressionKind.Call:
            {
                var callExpr = (BoundCallExpr)expression;

                var args = callExpr.Parameters.Select(BuildExpression).ToArray();
                if (args.Any(a => a.Kind == IRExpressionKind.Invalid))
                {
                    return invalidExpr;
                }

                var callee = BuildExpression(callExpr.Callee);
                if (callee.Kind == IRExpressionKind.Invalid)
                {
                    return invalidExpr;
                }

                // Free function call
                if (callee is IRNameExpr { Symbol: FunctionSymbol fn })
                {
                    var coerced = CoerceArgs(args, fn.Parameters, fn.NameId);
                    return coerced is null ? invalidExpr : new IRCallExpr(fn, coerced);
                }

                // Constructor call
                if (callee is IRNameExpr { Symbol: TypeNameSymbol } or IRMemberAccessExpr
                    {
                        MemberSymbol: TypeNameSymbol
                    })
                {
                    var ctor = callee.Type.Methods.FirstOrDefault(m =>
                        m.IsStatic && m.NameId == constructorNameId && m.Type == callee.Type);
                    if (ctor is null)
                    {
                        errors.Add(new TypeError(fileName, $"Type {interner[callee.Type.NameId]} has no constructor."));
                        return invalidExpr;
                    }
                    
                    var coerced = CoerceArgs(args, ctor.Parameters, ctor.NameId);
                    return coerced is null ? invalidExpr : new IRCallExpr(ctor, coerced);
                }

                // Method call
                if (callee is IRMemberAccessExpr { MemberSymbol: MethodSymbol method } ma)
                {
                    IRExpression[] actualArgs = method.IsStatic
                        ? args
                        : [ma.Receiver, .. args];

                    var coerced = CoerceArgs(actualArgs, method.Parameters, method.NameId);
                    return coerced is null ? invalidExpr : new IRCallExpr(method, coerced);
                }

                errors.Add(new TypeError(fileName, "Expression is not callable."));
                return invalidExpr;
            }
            case BoundExpressionKind.Conversion:
            {
                var conversionExpr = (BoundConversionExpr)expression;
                var irExpression = BuildExpression(conversionExpr.Expression);
                return new IRConversionExpr(irExpression, conversionExpr.TargetType);
            }
            case BoundExpressionKind.Initializer:
            {
                var init = (BoundInitializerExpr)expression;

                List<IRInitializer> irInits = [];
                HashSet<FieldSymbol> initialized = [];
                foreach (var i in init.Initializers)
                {
                    initialized.Add(i.Field);
                    
                    var v = BuildExpression(i.Value);
                    v = CoerceOrError(v, i.Field.Type);
                    if (v.Kind != IRExpressionKind.Invalid)
                    {
                        irInits.Add(new IRInitializer(i.Field, v));
                    }
                }
                
                foreach (var field in init.Type.Fields)
                {
                    if (initialized.Contains(field)) continue;
                    if (fieldDefaultValues.TryGetValue(field, out var value))
                    {
                        irInits.Add(new IRInitializer(field, value));
                    }
                }

                return new IRInitializerExpr(init.Type, irInits.ToArray());
            }
            default: return invalidExpr;
        }
    }

    private IRExpression CoerceOrError(IRExpression value, TypeSymbol targetType)
    {
        if (value.Type == targetType)
            return value;

        if (TypeSystem.CanImplicitlyConvert(value.Type, targetType))
            return new IRConversionExpr(value, targetType);

        errors.Add(new TypeError(fileName,
            $"Type mismatch assigning to {interner[targetType.NameId]}: expected {interner[targetType.NameId]}, got {interner[value.Type.NameId]}"));
        return invalidExpr;
    }

    private IRExpression[]? CoerceArgs(IRExpression[] args, TypeSymbol[] paramTypes, int calleeNameId)
    {
        if (args.Length != paramTypes.Length)
        {
            errors.Add(new TypeError(fileName,
                $"Arity mismatch calling {interner[calleeNameId]}: expected {paramTypes.Length}, got {args.Length}."));
            return null;
        }

        var coerced = new IRExpression[args.Length];

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            var expected = paramTypes[i];

            if (arg.Kind == IRExpressionKind.Invalid)
                return null;

            if (arg.Type == expected)
            {
                coerced[i] = arg;
                continue;
            }

            if (TypeSystem.CanImplicitlyConvert(arg.Type, expected))
            {
                coerced[i] = new IRConversionExpr(arg, expected);
                continue;
            }

            errors.Add(new TypeError(fileName,
                $"Type mismatch calling {interner[calleeNameId]} at argument #{i + 1}: " +
                $"expected {interner[expected.NameId]}, got {interner[arg.Type.NameId]}."));
            return null;
        }

        return coerced;
    }

    private void CheckConstantType(IRExpression initializer, TypeSymbol declaredType)
    {
        var initializerType = initializer.Type;
        if (initializerType != declaredType)
        {
            TypeError error = new(fileName,
                $"Type mismatch between {interner[declaredType.NameId]} and {interner[initializerType.NameId]}");
            errors.Add(error);
        }
    }

    private void CheckUnaryExpression(IRUnaryExpr unaryExpr)
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

    private void CheckLogicalExpression(IRLogicalExpr logicalExpr)
    {
        var leftType = logicalExpr.Left.Type;
        var rightType = logicalExpr.Right.Type;

        if (leftType.Kind != TypeKind.Bool || rightType.Kind != TypeKind.Bool)
        {
            errors.Add(new TypeError(fileName,
                $"Type mismatch; operator {logicalExpr.Operator} cannot be applied between types {interner[leftType.NameId]} and {interner[rightType.NameId]}"));
        }
    }

    private void CheckCondition(IRExpression condition)
    {
        var conditionType = condition.Type;
        if (conditionType.Kind != TypeKind.Bool)
        {
            errors.Add(new TypeError($"Type mismatch: condition must be bool, but got {interner[conditionType.NameId]}",
                fileName));
        }
    }

    private void CheckReturnStatementType(IRExpression? expression, TypeSymbol expected)
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

    private bool PromoteNumericPair(ref IRExpression l, ref IRExpression r, out TypeSymbol? common)
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

    private bool PromoteForEquality(ref IRExpression l, ref IRExpression r)
    {
        if (l.Type.IsNumeric && r.Type.IsNumeric) return PromoteNumericPair(ref l, ref r, out _);
        if (l.Type.Kind == TypeKind.Bool && r.Type.Kind == TypeKind.Bool) return true;
        if (l.Type.Kind == TypeKind.String && r.Type.Kind == TypeKind.String) return true;
        return false;
    }

    private IRExpression BinaryInvalid(BinaryOp op, IRExpression l, IRExpression r)
    {
        errors.Add(new TypeError(fileName,
            $"Operator {op} cannot be applied between types '{interner[l.Type.NameId]}' and '{interner[r.Type.NameId]}'"));
        return invalidExpr;
    }
}
