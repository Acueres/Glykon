using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Binding.BoundExpressions;
using Glykon.Compiler.Semantics.Binding.BoundStatements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;
using System.Reflection;
using System.Reflection.Emit;

namespace Glykon.Compiler.Emitter;

internal class MethodEmitter
{
    readonly MethodBuilder mb;
    readonly ILGenerator il;

    readonly BoundFunctionDeclaration fStmt;
    readonly string appName;

    Dictionary<FunctionSymbol, MethodInfo> combinedMethods = [];
    readonly Dictionary<FunctionSymbol, MethodInfo> localFunctions = [];
    readonly List<MethodEmitter> methodGenerators = [];

    readonly Label? returnLabel;
    readonly LocalBuilder? returnLocal;

    //readonly Stack<TokenKind[]> functionParameters = [];
    readonly Stack<Label> loopStart = [];
    readonly Stack<Label> loopEnd = [];

    public MethodEmitter(BoundFunctionDeclaration stmt, IdentifierInterner interner, TypeBuilder typeBuilder, string appName)
    {
        fStmt = stmt;
        this.appName = appName;

        var parameterTypes = TranslateTypes([.. stmt.Parameters.Select(p => p.Type)]);
        var returnType = TranslateType(stmt.ReturnType);

        string name = interner[stmt.Signature.QualifiedId];

        mb = typeBuilder.DefineMethod(name,
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
            returnType, parameterTypes);

        il = mb.GetILGenerator();

        for (int i = 0; i < stmt.Parameters.Length; i++)
        {
            string paramName = interner[stmt.Parameters[i].Id];
            mb.DefineParameter(i + 1, ParameterAttributes.None, paramName);
        }

        int n = CountReturnStatements(stmt);
        bool multipleReturns = n > 1;

        if (multipleReturns || (stmt.ReturnType == TokenKind.None && n > 0))
        {
            returnLabel = il.DefineLabel();
        }

        if (stmt.ReturnType != TokenKind.None && multipleReturns)
        {
            returnLocal = il.DeclareLocal(TranslateType(stmt.ReturnType));
        }

        var locals = stmt.Body.Statements.Where(s => s.Kind == StatementKind.Function).Select(s => (BoundFunctionDeclaration)s);
        foreach (var f in locals)
        {
            MethodEmitter mg = new(f, interner, typeBuilder, appName);
            methodGenerators.Add(mg);
            localFunctions.Add(f.Signature, mg.GetMethodBuilder());
        }
    }

    public MethodBuilder GetMethodBuilder() => mb;

    public void EmitMethod(Dictionary<FunctionSymbol, MethodInfo> methods)
    {
        combinedMethods = methods.Concat(localFunctions).ToDictionary();
        
        foreach (BoundStatement stmt in fStmt.Body.Statements)
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

    void EmitStatement(BoundStatement statement)
    {
        switch (statement.Kind)
        {
            case StatementKind.Variable:
                EmitVariableDeclarationStatement((BoundVariableDeclaration)statement);
                break;
            case StatementKind.If:
                EmitIfStatement((BoundIfStmt)statement);
                break;
            case StatementKind.While:
                EmitWhileStatement((BoundWhileStmt)statement);
                break;
            case StatementKind.Block:
                EmitBlockStatement((BoundBlockStmt)statement);
                break;
            case StatementKind.Jump:
                EmitJumpStatement((BoundJumpStmt)statement);
                break;
            case StatementKind.Return:
                EmitReturnStatement((BoundReturnStmt)statement);
                break;
            case StatementKind.Function:
            case StatementKind.Constant:  break;
            default:
                EmitExpression(((BoundExpressionStmt)statement).Expression);
                break;
        }
    }

    void EmitBlockStatement(BoundBlockStmt blockStmt)
    {
        foreach (var s in blockStmt.Statements)
        {
            EmitStatement(s);
        }
    }

    void EmitVariableDeclarationStatement(BoundVariableDeclaration statement)
    {
        VariableSymbol symbol = statement.Symbol;

        Type type = TranslateType(symbol.Type);

        var lb = il.DeclareLocal(type);
        symbol.LocalIndex = lb.LocalIndex;

        EmitExpression(statement.Expression);
        il.Emit(OpCodes.Stloc, symbol.LocalIndex);
    }

    void EmitReturnStatement(BoundReturnStmt returnStmt)
    {
        if (returnStmt.Expression != null)
        {
            EmitExpression(returnStmt.Expression);

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

    void EmitIfStatement(BoundIfStmt ifStmt)
    {
        EmitExpression(ifStmt.Expression);

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

    void EmitWhileStatement(BoundWhileStmt whileStmt)
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

    void EmitJumpStatement(BoundJumpStmt jumpStatement)
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

    TokenKind EmitExpression(BoundExpression expression)
    {
        switch (expression.Kind)
        {
            case ExpressionKind.Literal:
                {
                    var expr = (BoundLiteralExpr)expression;
                    return EmitPrimitive(expr.Token);
                }
            case ExpressionKind.Variable:
                {
                    var expr = (BoundVariableExpr)expression;

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

                    return TokenKind.None;
                }
            case ExpressionKind.Assignment:
                {
                    var expr = (BoundAssignmentExpr)expression;
                    
                    if (expr.Symbol is not VariableSymbol variableSymbol)
                    {
                        return TokenKind.None;
                    }

                    EmitExpression(expr.Right);
                    il.Emit(OpCodes.Stloc, variableSymbol.LocalIndex);

                    return variableSymbol.Type;
                }
            case ExpressionKind.Call:
                {
                    var expr = (BoundCallExpr)expression;

                    TokenKind[] parameters = new TokenKind[expr.Args.Length];
                    int i = 0;
                    foreach (var arg in expr.Args)
                    {
                        TokenKind type = EmitExpression(arg);
                        parameters[i++] = type;
                    }
                    
                    var function = expr.Function;

                    il.EmitCall(OpCodes.Call, combinedMethods[function], []);

                    return function.Type;
                }
            case ExpressionKind.Unary:
                {
                    var expr = (BoundUnaryExpr)expression;
                    var type = EmitExpression(expr.Operand);

                    switch (expr.Operator.Kind)
                    {
                        case TokenKind.Not when type == TokenKind.Bool:
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            return TokenKind.Bool;
                        case TokenKind.Minus when type == TokenKind.Int || type == TokenKind.Real:
                            il.Emit(OpCodes.Neg);
                            return type;
                    }

                    break;
                }
            case ExpressionKind.Binary:
                {
                    var expr = (BoundBinaryExpr)expression;
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
                            return TokenKind.Bool;
                        case TokenKind.NotEqual:
                            il.Emit(OpCodes.Ceq);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            return TokenKind.Bool;
                        case TokenKind.Greater when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Cgt);
                            return TokenKind.Bool;
                        case TokenKind.GreaterEqual when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Clt);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            return TokenKind.Bool;
                        case TokenKind.Less when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Clt);
                            return TokenKind.Bool;
                        case TokenKind.LessEqual when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Cgt);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            il.Emit(OpCodes.Ceq);
                            return TokenKind.Bool;
                        case TokenKind.Plus when typeLeft == TokenKind.String && typeRight == TokenKind.String:
                            il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]), []);
                            return TokenKind.String;
                        case TokenKind.Plus when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Add);
                            return typeLeft;
                        case TokenKind.Minus when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Sub);
                            return typeLeft;
                        case TokenKind.Slash when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Div);
                            return typeLeft;
                        case TokenKind.Star when typeLeft == TokenKind.Int || typeLeft == TokenKind.Real:
                            il.Emit(OpCodes.Mul);
                            return typeLeft;
                    }

                    break;
                }

            case ExpressionKind.Logical:
                {
                    var expr = (BoundLogicalExpr)expression;

                    if ((expr.Left.Kind == ExpressionKind.Literal || expr.Left.Kind == ExpressionKind.Variable)
                        && (expr.Right.Kind == ExpressionKind.Literal || expr.Right.Kind == ExpressionKind.Variable))
                    {
                        TokenKind typeLeft = EmitExpression(expr.Left);
                        TokenKind typeRight = EmitExpression(expr.Right);

                        if (typeLeft != TokenKind.Bool && typeLeft != typeRight)
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
                        TokenKind typeLeft = EmitExpression(expr.Left);

                        if (expr.Operator.Kind == TokenKind.And)
                        {
                            Label leftTrue = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, leftTrue);
                            il.Emit(OpCodes.Ldc_I4, 0);
                            Label endLabel = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, endLabel);

                            il.MarkLabel(leftTrue);
                            TokenKind typeRight = EmitExpression(expr.Right);

                            if (typeLeft != TokenKind.Bool && typeLeft != typeRight)
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
                            TokenKind typeRight = EmitExpression(expr.Right);

                            if (typeLeft != TokenKind.Bool && typeLeft != typeRight)
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

                    return TokenKind.Bool;
                }
        }

        return TokenKind.None;
    }

    TokenKind EmitPrimitive(in Token token)
    {
        switch (token.Kind)
        {
            case TokenKind.LiteralString: il.Emit(OpCodes.Ldstr, token.StringValue ?? ""); return TokenKind.String;
            case TokenKind.LiteralInt: il.Emit(OpCodes.Ldc_I4, (int)token.IntValue); return TokenKind.Int;
            case TokenKind.LiteralReal: il.Emit(OpCodes.Ldc_R8, token.RealValue); return TokenKind.Real;
            case TokenKind.LiteralTrue: il.Emit(OpCodes.Ldc_I4, 1); return TokenKind.Bool;
            case TokenKind.LiteralFalse: il.Emit(OpCodes.Ldc_I4, 0); return TokenKind.Bool;
            default: il.Emit(OpCodes.Ldnull); return TokenKind.None;
        }
    }

    static Type TranslateType(TokenKind type)
    {
        return type switch
        {
            TokenKind.Bool => typeof(bool),
            TokenKind.LiteralTrue => typeof(bool),
            TokenKind.LiteralFalse => typeof(bool),
            TokenKind.Int => typeof(int),
            TokenKind.Real => typeof(double),
            TokenKind.String => typeof(string),
            _ => typeof(void),
        };
    }

    static Type[] TranslateTypes(TokenKind[] types)
    {
        List<Type> result = new(types.Length);

        foreach (var type in types)
        {
            result.Add(TranslateType(type));
        }

        return [.. result];
    }

    static int CountReturnStatements(BoundStatement statement)
    {
        if (statement.Kind == StatementKind.Return)
        {
            return 1;
        }

        int count = 0;
        if (statement is BoundFunctionDeclaration fStmt)
        {
            count += CountReturnStatements(fStmt.Body);
        }
        else if (statement is BoundBlockStmt blockStmt)
        {
            foreach (var stmt in blockStmt.Statements)
            {
                count += CountReturnStatements(stmt);
            }
        }
        else if (statement is BoundIfStmt ifStmt)
        {
            count += CountReturnStatements(ifStmt.ThenStatement);

            if (ifStmt.ElseStatement is not null)
            {
                count += CountReturnStatements(ifStmt.ElseStatement);
            }
        }
        else if (statement is BoundWhileStmt whileStmt)
        {
            count += CountReturnStatements(whileStmt.Body);
        }

        return count;
    }
}
