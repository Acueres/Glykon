using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Operators;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public class CilCodeGenerator(ILGenerator il, TypeSystem typeSystem, CilEmitContext context)
{
    private readonly Stack<Label> loopStart = [];
    private readonly Stack<Label> loopEnd = [];

    public void EmitStatements(IRStatement[] statements)
    {
        foreach (var statement in statements)
        {
            EmitStatement(statement);
        }
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
            case IRStatementKind.Expression:
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

        Type type = IntrinsicClrTypeTranslator.Translate(symbol.Type);

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

            if (context.ReturnLocal != null)
            {
                il.Emit(OpCodes.Stloc, context.ReturnLocal.LocalIndex);
            }
        }

        if (context.ReturnLabel is not null)
        {
            il.Emit(OpCodes.Br_S, (Label)context.ReturnLabel);
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

    void EmitExpression(IRExpression expression)
    {
        while (true)
        {
            switch (expression.Kind)
            {
                case IRExpressionKind.Literal:
                {
                    var expr = (IRLiteralExpr)expression;
                    EmitPrimitive(expr.Value);
                    break;
                }
                case IRExpressionKind.Name:
                {
                    var expr = (IRNameExpr)expression;

                    Symbol symbol = expr.Symbol;

                    if (symbol is ParameterSymbol parameter)
                    {
                        il.Emit(OpCodes.Ldarg, parameter.Index);
                        break;
                    }

                    if (symbol is VariableSymbol variable)
                    {
                        il.Emit(OpCodes.Ldloc, variable.LocalIndex);
                        break;
                    }

                    if (symbol is ConstantSymbol constant)
                    {
                        EmitPrimitive(constant.Value);
                        break;
                    }

                    break;
                }
                case IRExpressionKind.Assignment:
                {
                    var expr = (IRAssignmentExpr)expression;

                    if (expr.Symbol is not VariableSymbol variableSymbol)
                    {
                        break;
                    }

                    EmitExpression(expr.Value);
                    il.Emit(OpCodes.Stloc, variableSymbol.LocalIndex);

                    break;
                }
                case IRExpressionKind.Call:
                {
                    var expr = (IRCallExpr)expression;

                    foreach (var arg in expr.Parameters)
                    {
                        EmitExpression(arg);
                    }

                    MethodInfo methodInfo;
                    if (expr.Callable is MethodSymbol method && context.Methods is not null)
                    {
                        methodInfo = context.Methods[method];
                    }
                    else
                    {
                        methodInfo = context.Functions[(FunctionSymbol)expr.Callable];
                    }

                    il.EmitCall(OpCodes.Call, methodInfo, []);

                    break;
                }
                case IRExpressionKind.Unary:
                {
                    var expr = (IRUnaryExpr)expression;
                    var type = expr.Operand.Type;

                    switch (expr.Operator)
                    {
                        case UnaryOp.LogicalNot when type.Kind == TypeKind.Bool:
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case UnaryOp.Minus when type.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Neg);
                            break;
                    }

                    break;
                }
                case IRExpressionKind.Binary:
                {
                    var expr = (IRBinaryExpr)expression;
                    EmitExpression(expr.Left);
                    EmitExpression(expr.Right);

                    var typeLeft = expr.Left.Type;
                    var typeRight = expr.Right.Type;

                    switch (expr.Operator)
                    {
                        case BinaryOp.Equal:
                            il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOp.NotEqual:
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOp.Greater when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Cgt);
                            break;
                        case BinaryOp.GreaterOrEqual when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Clt);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOp.Less when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Clt);
                            break;
                        case BinaryOp.LessOrEqual when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Cgt);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            break;
                        case BinaryOp.Add when typeLeft.Kind == TypeKind.String && typeRight.Kind == TypeKind.String:
                            il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]), []);
                            break;
                        case BinaryOp.Add when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Add);
                            break;
                        case BinaryOp.Subtract when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Sub);
                            break;
                        case BinaryOp.Divide when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Div);
                            break;
                        case BinaryOp.Multiply when typeLeft.Kind is TypeKind.Int64 or TypeKind.Float64:
                            il.Emit(OpCodes.Mul);
                            break;
                    }

                    break;
                }

                case IRExpressionKind.Logical:
                {
                    var expr = (IRLogicalExpr)expression;

                    if (expr.Left.Kind is IRExpressionKind.Literal or IRExpressionKind.Name && expr.Right.Kind is IRExpressionKind.Literal or IRExpressionKind.Name)
                    {
                        EmitExpression(expr.Left);
                        EmitExpression(expr.Right);

                        switch (expr.Operator)
                        {
                            case BinaryOp.LogicalAnd:
                                il.Emit(OpCodes.And);
                                break;
                            case BinaryOp.LogicalOr:
                                il.Emit(OpCodes.Or);
                                break;
                        }
                    }
                    else
                    {
                        EmitExpression(expr.Left);

                        if (expr.Operator == BinaryOp.LogicalAnd)
                        {
                            Label leftTrue = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, leftTrue);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            Label endLabel = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, endLabel);

                            il.MarkLabel(leftTrue);
                            EmitExpression(expr.Right);

                            il.MarkLabel(endLabel);
                        }
                        else
                        {
                            Label leftTrue = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, leftTrue);
                            EmitExpression(expr.Right);

                            Label endLabel = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, endLabel);

                            il.MarkLabel(leftTrue);
                            il.Emit(OpCodes.Ldc_I4, 1);

                            il.MarkLabel(endLabel);
                        }
                    }

                    break;
                }
                case IRExpressionKind.Conversion:
                {
                    var conversionExpr = (IRConversionExpr)expression;
                    EmitExpression(conversionExpr.Expression);

                    var from = conversionExpr.Expression.Type;
                    if (from == conversionExpr.Type) break;
                    // widening int64 to float64
                    if (from.Kind == TypeKind.Int64 && conversionExpr.Type.Kind == TypeKind.Float64)
                    {
                        il.Emit(OpCodes.Conv_R8);
                    }

                    // cast to string
                    if (conversionExpr.Type.Kind == TypeKind.String && from.Kind != TypeKind.String)
                    {
                        switch (from.Kind)
                        {
                            case TypeKind.Int64:
                                il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(long)])!);
                                break;
                            case TypeKind.Float64:
                                il.Emit(OpCodes.Call, typeof(System.Globalization.CultureInfo).GetProperty(nameof(System.Globalization.CultureInfo.InvariantCulture))!.GetGetMethod()!);

                                il.Emit(OpCodes.Call, typeof(Convert).GetMethod(nameof(Convert.ToString), [typeof(double), typeof(IFormatProvider)])!);
                                break;
                            case TypeKind.Bool:
                            {
                                var lblTrue = il.DefineLabel();
                                var lblEnd = il.DefineLabel();

                                il.Emit(OpCodes.Brtrue_S, lblTrue);
                                il.Emit(OpCodes.Ldstr, "False");
                                il.Emit(OpCodes.Br_S, lblEnd);

                                il.MarkLabel(lblTrue);
                                il.Emit(OpCodes.Ldstr, "True");

                                il.MarkLabel(lblEnd);
                                break;
                            }
                        }
                    }

                    break;
                }
                case IRExpressionKind.Grouping:
                {
                    var groupExpr = (IRGroupingExpr)expression;
                    expression = groupExpr.Expression;
                    continue;
                }
            }

            break;
        }
    }

    void EmitPrimitive(in ConstantValue value)
    {
        switch (value.Kind)
        {
            case ConstantKind.String: il.Emit(OpCodes.Ldstr, value.String); break;
            case ConstantKind.Int: il.Emit(OpCodes.Ldc_I8, value.Int); break;
            case ConstantKind.Real: il.Emit(OpCodes.Ldc_R8, value.Real); break;
            case ConstantKind.Bool: il.Emit(OpCodes.Ldc_I4, value.Bool ? 1 : 0); break;
            case ConstantKind.None: il.Emit(OpCodes.Ldnull); break;
        }
    }
}