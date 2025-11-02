using System.Reflection;
using System.Reflection.Emit;
using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Emitter;

internal class FunctionEmitter
{
    readonly MethodBuilder mb;
    readonly ILGenerator il;
    readonly TypeSystem typeSystem;

    readonly IRFunctionDeclaration fStmt;
    readonly string appName;

    Dictionary<FunctionSymbol, MethodInfo> combinedMethods = [];
    readonly Dictionary<FunctionSymbol, MethodInfo> localFunctions = [];
    readonly List<FunctionEmitter> methodGenerators = [];

    readonly Label? returnLabel;
    readonly LocalBuilder? returnLocal;

    readonly Stack<Label> loopStart = [];
    readonly Stack<Label> loopEnd = [];

    public FunctionEmitter(IRFunctionDeclaration stmt, TypeSystem typeSystem, IdentifierInterner interner,
        TypeBuilder typeBuilder, string appName)
    {
        fStmt = stmt;
        this.appName = appName;
        this.typeSystem = typeSystem;

        var parameterTypes = TranslateTypes([.. stmt.Parameters.Select(p => p.Type)]);
        var returnType = TranslateType(stmt.ReturnType);

        string name = interner[stmt.Signature.QualifiedNameId];

        mb = typeBuilder.DefineMethod(name,
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
            returnType, parameterTypes);

        il = mb.GetILGenerator();

        for (int i = 0; i < stmt.Parameters.Length; i++)
        {
            string paramName = interner[stmt.Parameters[i].NameId];
            mb.DefineParameter(i + 1, ParameterAttributes.None, paramName);
        }

        int n = CountReturnStatements(stmt);
        bool multipleReturns = n > 1;

        if (multipleReturns || (stmt.ReturnType.Kind == TypeKind.None && n > 0))
        {
            returnLabel = il.DefineLabel();
        }

        if (stmt.ReturnType.Kind != TypeKind.None && multipleReturns)
        {
            returnLocal = il.DeclareLocal(TranslateType(stmt.ReturnType));
        }

        var locals = stmt.Body.Statements
            .Where(s => s.Kind == IRStatementKind.Function).Cast<IRFunctionDeclaration>();
        foreach (var f in locals)
        {
            FunctionEmitter mg = new(f, typeSystem, interner, typeBuilder, appName);
            methodGenerators.Add(mg);
            localFunctions.Add(f.Signature, mg.GetMethodBuilder());
        }
    }

    public MethodBuilder GetMethodBuilder() => mb;

    public void EmitMethod(Dictionary<FunctionSymbol, MethodInfo> methods)
    {
        combinedMethods = methods.Concat(localFunctions).ToDictionary();
        
        foreach (var stmt in fStmt.Body.Statements)
        {
            EmitStatement(stmt);
        }

        foreach (var mg in methodGenerators)
        {
            mg.EmitMethod(combinedMethods);
        }

        if (returnLabel is not null)
        {
            il.MarkLabel((Label)returnLabel);
        }

        if (returnLocal is not null)
        {
            il.Emit(OpCodes.Ldloc, returnLocal.LocalIndex);
        }

        il.Emit(OpCodes.Ret);
    }

    void EmitStatement(IRStatement statement)
    {
        switch (statement.Kind)
        {
            case IRStatementKind.Variable:
                EmitVariableDeclarationStatement((IRVariableDeclaration)statement);
                break;
            case IRStatementKind.If:
                EmitIfStatement((IRIfStmt)statement);
                break;
            case IRStatementKind.While:
                EmitWhileStatement((IRWhileStmt)statement);
                break;
            case IRStatementKind.Block:
                EmitBlockStatement((IRBlockStmt)statement);
                break;
            case IRStatementKind.Jump:
                EmitJumpStatement((IRJumpStmt)statement);
                break;
            case IRStatementKind.Return:
                EmitReturnStatement((IRReturnStmt)statement);
                break;
            case IRStatementKind.Function:
            case IRStatementKind.Constant:  break;
            default:
                EmitExpression(((IRExpressionStmt)statement).Expression);
                break;
        }
    }

    void EmitBlockStatement(IRBlockStmt blockStmt)
    {
        foreach (var s in blockStmt.Statements)
        {
            EmitStatement(s);
        }
    }

    void EmitVariableDeclarationStatement(IRVariableDeclaration statement)
    {
        VariableSymbol symbol = statement.Symbol;

        Type type = TranslateType(symbol.Type);

        var lb = il.DeclareLocal(type);
        symbol.LocalIndex = lb.LocalIndex;

        EmitExpression(statement.Initializer);
        il.Emit(OpCodes.Stloc, symbol.LocalIndex);
    }

    void EmitReturnStatement(IRReturnStmt returnStmt)
    {
        if (returnStmt.Value != null)
        {
            EmitExpression(returnStmt.Value);

            if (returnLocal != null)
            {
                il.Emit(OpCodes.Stloc, returnLocal.LocalIndex);
            }
        }

        if (returnLabel is not null)
        {
            il.Emit(OpCodes.Br_S, (Label)returnLabel);
        }
    }

    void EmitIfStatement(IRIfStmt ifStmt)
    {
        EmitExpression(ifStmt.Condition);

        if (ifStmt.ElseStatement is not null)
        {
            Label elseLabel = il.DefineLabel();

            il.Emit(OpCodes.Brfalse_S, elseLabel);

            EmitStatement(ifStmt.ThenStatement);

            Label endLabel = il.DefineLabel();
            il.Emit(OpCodes.Br_S, endLabel);

            il.MarkLabel(elseLabel);

            EmitStatement(ifStmt.ElseStatement);

            il.MarkLabel(endLabel);
        }
        else
        {
            Label endLabel = il.DefineLabel();

            il.Emit(OpCodes.Brfalse_S, endLabel);

            EmitStatement(ifStmt.ThenStatement);

            il.MarkLabel(endLabel);
        }
    }

    void EmitWhileStatement(IRWhileStmt whileStmt)
    {
        var loopStartLabel = il.DefineLabel();
        var loopEndLabel = il.DefineLabel();

        loopStart.Push(loopStartLabel);
        loopEnd.Push(loopEndLabel);

        il.MarkLabel(loopStartLabel);
        EmitExpression(whileStmt.Condition);

        il.Emit(OpCodes.Brfalse_S, loopEndLabel);

        EmitStatement(whileStmt.Body);

        il.Emit(OpCodes.Br_S, loopStartLabel);

        il.MarkLabel(loopEndLabel);

        loopStart.Pop();
        loopEnd.Pop();
    }

    void EmitJumpStatement(IRJumpStmt jumpStatement)
    {
        if (jumpStatement.IsBreak)
        {
            il.Emit(OpCodes.Br_S, loopEnd.Last());
        }
        else if (jumpStatement.IsContinue)
        {
            il.Emit(OpCodes.Br_S, loopStart.Last());
        }
    }

    TypeSymbol EmitExpression(IRExpression expression)
    {
        switch (expression.Kind)
        {
            case IRExpressionKind.Literal:
            {
                var expr = (IRLiteralExpr)expression;
                return EmitPrimitive(expr.Value);
            }
            case IRExpressionKind.Variable:
            {
                var expr = (IRVariableExpr)expression;

                Symbol symbol = expr.Symbol;

                if (symbol is ParameterSymbol parameter)
                {
                    il.Emit(OpCodes.Ldarg, parameter.Index);
                    return parameter.Type;
                }

                if (symbol is VariableSymbol variable)
                {
                    il.Emit(OpCodes.Ldloc, variable.LocalIndex);

                    return variable.Type;
                }

                if (symbol is ConstantSymbol constant)
                {
                    return EmitPrimitive(constant.Value);
                }

                return typeSystem[TypeKind.None];
            }
            case IRExpressionKind.Assignment:
            {
                var expr = (IRAssignmentExpr)expression;

                if (expr.Symbol is not VariableSymbol variableSymbol)
                {
                    return typeSystem[TypeKind.None];
                }

                EmitExpression(expr.Value);
                il.Emit(OpCodes.Stloc, variableSymbol.LocalIndex);

                return variableSymbol.Type;
            }
            case IRExpressionKind.Call:
            {
                var expr = (IRCallExpr)expression;
                
                foreach (var arg in expr.Parameters)
                {
                    EmitExpression(arg);
                }

                var function = expr.Function;

                il.EmitCall(OpCodes.Call, combinedMethods[function], []);

                return function.Type;
            }
            case IRExpressionKind.Unary:
            {
                var expr = (IRUnaryExpr)expression;
                var type = EmitExpression(expr.Operand);

                switch (expr.Operator.Kind)
                {
                    case TokenKind.Not when type.Kind == TypeKind.Bool:
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Ceq);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.Minus when type.Kind == TypeKind.Int64 || type.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Neg);
                        return type;
                }

                break;
            }
            case IRExpressionKind.Binary:
            {
                var expr = (IRBinaryExpr)expression;
                var typeLeft = EmitExpression(expr.Left);
                var typeRight = EmitExpression(expr.Right);

                if (typeLeft != typeRight)
                {
                    ParseError error = new(expr.Operator, appName,
                        $"Operator {expr.Operator.Kind} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                    throw error.Exception();
                }

                switch (expr.Operator.Kind)
                {
                    case TokenKind.Equal:
                        il.Emit(OpCodes.Ceq);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.NotEqual:
                        il.Emit(OpCodes.Ceq);
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Ceq);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.Greater when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Cgt);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.GreaterEqual
                        when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Clt);
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Ceq);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.Less when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Clt);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.LessEqual when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Cgt);
                        il.Emit(OpCodes.Ldc_I4, 0);
                        il.Emit(OpCodes.Ceq);
                        return typeSystem[TypeKind.Bool];
                    case TokenKind.Plus when typeLeft.Kind == TypeKind.String && typeRight.Kind == TypeKind.String:
                        il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]),
                            []);
                        return typeSystem[TypeKind.String];
                    case TokenKind.Plus when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Add);
                        return typeLeft;
                    case TokenKind.Minus when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Sub);
                        return typeLeft;
                    case TokenKind.Slash when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Div);
                        return typeLeft;
                    case TokenKind.Star when typeLeft.Kind == TypeKind.Int64 || typeLeft.Kind == TypeKind.Float64:
                        il.Emit(OpCodes.Mul);
                        return typeLeft;
                }

                break;
            }

            case IRExpressionKind.Logical:
            {
                var expr = (IRLogicalExpr)expression;

                if (expr.Left.Kind is IRExpressionKind.Literal or IRExpressionKind.Variable
                    && expr.Right.Kind is IRExpressionKind.Literal or IRExpressionKind.Variable)
                {
                    var typeLeft = EmitExpression(expr.Left);
                    var typeRight = EmitExpression(expr.Right);

                    if (typeLeft.Kind != TypeKind.Bool && typeLeft != typeRight)
                    {
                        ParseError error = new(expr.Operator, appName,
                            $"Operator {expr.Operator.Kind} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                        throw error.Exception();
                    }

                    if (expr.Operator.Kind == TokenKind.And)
                    {
                        il.Emit(OpCodes.And);
                    }
                    else
                    {
                        il.Emit(OpCodes.Or);
                    }
                }
                else
                {
                    var typeLeft = EmitExpression(expr.Left);

                    if (expr.Operator.Kind == TokenKind.And)
                    {
                        Label leftTrue = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, leftTrue);
                        il.Emit(OpCodes.Ldc_I4, 0);
                        Label endLabel = il.DefineLabel();
                        il.Emit(OpCodes.Br_S, endLabel);

                        il.MarkLabel(leftTrue);
                        var typeRight = EmitExpression(expr.Right);

                        if (typeLeft.Kind != TypeKind.Bool && typeLeft != typeRight)
                        {
                            ParseError error = new(expr.Operator, appName,
                                $"Operator {expr.Operator.Kind} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                            throw error.Exception();
                        }

                        il.MarkLabel(endLabel);
                    }
                    else
                    {
                        Label leftTrue = il.DefineLabel();
                        il.Emit(OpCodes.Brtrue_S, leftTrue);
                        var typeRight = EmitExpression(expr.Right);

                        if (typeLeft.Kind != TypeKind.Bool && typeLeft != typeRight)
                        {
                            ParseError error = new(expr.Operator, appName,
                                $"Operator {expr.Operator.Kind} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                            throw error.Exception();
                        }

                        Label endLabel = il.DefineLabel();
                        il.Emit(OpCodes.Br_S, endLabel);

                        il.MarkLabel(leftTrue);
                        il.Emit(OpCodes.Ldc_I4, 1);

                        il.MarkLabel(endLabel);
                    }
                }

                return typeSystem[TypeKind.Bool];
            }

            case IRExpressionKind.Conversion:
            {
                var conversionExpr = (IRConversionExpr)expression;
                var from = EmitExpression(conversionExpr.Expression);
                if (from == conversionExpr.Type) return conversionExpr.Type;
                // widening int64 to float64
                if (from.Kind == TypeKind.Int64 && conversionExpr.Type.Kind == TypeKind.Float64)
                    il.Emit(OpCodes.Conv_R8);
                
                return conversionExpr.Type;
            }
        }

        return typeSystem[TypeKind.None];
    }

    TypeSymbol EmitPrimitive(in ConstantValue value)
    {
        switch (value.Kind)
        {
            case ConstantKind.String: il.Emit(OpCodes.Ldstr, value.String); return typeSystem[TypeKind.String];
            case ConstantKind.Int: il.Emit(OpCodes.Ldc_I8, value.Int); return typeSystem[TypeKind.Int64];
            case ConstantKind.Real: il.Emit(OpCodes.Ldc_R8, value.Real); return typeSystem[TypeKind.Float64];
            case ConstantKind.Bool: il.Emit(OpCodes.Ldc_I4, value.Bool ? 1 : 0); return typeSystem[TypeKind.Bool];
            default: il.Emit(OpCodes.Ldnull); return typeSystem[TypeKind.None];
        }
    }

    static Type TranslateType(TypeSymbol type)
    {
        return type.Kind switch
        {
            TypeKind.Bool => typeof(bool),
            TypeKind.Int64 => typeof(long),
            TypeKind.Float64 => typeof(double),
            TypeKind.String => typeof(string),
            _ => typeof(void),
        };
    }

    static Type[] TranslateTypes(TypeSymbol[] types)
    {
        List<Type> result = new(types.Length);

        foreach (var type in types)
        {
            result.Add(TranslateType(type));
        }

        return [.. result];
    }

    static int CountReturnStatements(IRStatement statement)
    {
        if (statement.Kind == IRStatementKind.Return)
        {
            return 1;
        }

        int count = 0;
        if (statement is IRFunctionDeclaration fStmt)
        {
            count += CountReturnStatements(fStmt.Body);
        }
        else if (statement is IRBlockStmt blockStmt)
        {
            foreach (var stmt in blockStmt.Statements)
            {
                count += CountReturnStatements(stmt);
            }
        }
        else if (statement is IRIfStmt ifStmt)
        {
            count += CountReturnStatements(ifStmt.ThenStatement);

            if (ifStmt.ElseStatement is not null)
            {
                count += CountReturnStatements(ifStmt.ElseStatement);
            }
        }
        else if (statement is IRWhileStmt whileStmt)
        {
            count += CountReturnStatements(whileStmt.Body);
        }

        return count;
    }
}
