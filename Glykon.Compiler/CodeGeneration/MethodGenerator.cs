using System.Reflection;
using System.Reflection.Emit;
using Glykon.Compiler.Tokenization;
using Glykon.Compiler.SemanticAnalysis;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.SemanticAnalysis.Symbols;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.CodeGeneration
{
    internal class MethodGenerator
    {
        readonly MethodBuilder mb;
        readonly ILGenerator il;

        readonly FunctionStmt fStmt;
        readonly SymbolTable st;
        readonly string appName;

        Dictionary<FunctionSymbol, MethodInfo> combinedMethods = [];
        readonly Dictionary<FunctionSymbol, MethodInfo> localFunctions = [];
        readonly List<MethodGenerator> methodGenerators = [];

        readonly Label? returnLabel;
        readonly LocalBuilder? returnLocal;

        readonly Stack<TokenType[]> functionParameters = [];
        readonly Stack<Label> loopStart = [];
        readonly Stack<Label> loopEnd = [];

        public MethodGenerator(FunctionStmt stmt, SymbolTable symbolTable, TypeBuilder typeBuilder, string appName)
        {
            var parameterTypes = TranslateTypes([.. stmt.Parameters.Select(p => p.Type)]);
            var returnType = TranslateType(stmt.ReturnType);

            mb = typeBuilder.DefineMethod(stmt.Name,
                MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
                returnType, parameterTypes);

            for (int i = 0; i < stmt.Parameters.Count; i++)
            {
                mb.DefineParameter(i + 1, ParameterAttributes.None, stmt.Parameters[i].Name);
            }

            il = mb.GetILGenerator();

            fStmt = stmt;
            st = symbolTable;
            this.appName = appName;

            int n = CountReturnStatements(stmt);
            bool multipleReturns = n > 1;

            if (multipleReturns || (stmt.ReturnType == TokenType.None && n > 0))
            {
                returnLabel = il.DefineLabel();
            }

            if (stmt.ReturnType != TokenType.None && multipleReturns)
            {
                returnLocal = il.DeclareLocal(TranslateType(stmt.ReturnType));
            }

            var locals = stmt.Body.Statements.Where(s => s.Type == StatementType.Function).Select(s => (FunctionStmt)s);
            foreach (var f in locals)
            {
                MethodGenerator mg = new(f, symbolTable, typeBuilder, appName);
                methodGenerators.Add(mg);
                localFunctions.Add(f.Signature, mg.GetMethodBuilder());
            }
        }

        public MethodBuilder GetMethodBuilder() => mb;

        public void EmitMethod(Dictionary<FunctionSymbol, MethodInfo> methods)
        {
            combinedMethods = methods.Concat(localFunctions).ToDictionary();

            st.EnterScope(fStmt.Body.ScopeIndex);

            foreach (IStatement stmt in fStmt.Body.Statements)
            {
                EmitStatement(stmt);
            }

            foreach (var mg in methodGenerators)
            {
                mg.EmitMethod(combinedMethods);
            }

            st.ExitScope();

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

        void EmitStatement(IStatement statement)
        {
            switch (statement.Type)
            {
                case StatementType.Variable:
                    EmitVariableDeclarationStatement((VariableStmt)statement);
                    break;
                case StatementType.If:
                    EmitIfStatement((IfStmt)statement);
                    break;
                case StatementType.While:
                    EmitWhileStatement((WhileStmt)statement);
                    break;
                case StatementType.Block:
                    EmitBlockStatement((BlockStmt)statement);
                    break;
                case StatementType.Jump:
                    EmitJumpStatement((JumpStmt)statement);
                    break;
                case StatementType.Return:
                    EmitReturnStatement((ReturnStmt)statement);
                    break;
                case StatementType.Function: break;
                default:
                    EmitExpression(statement.Expression);
                    break;
            }
        }

        void EmitBlockStatement(BlockStmt blockStmt)
        {
            st.EnterScope(blockStmt.ScopeIndex);

            foreach (var s in blockStmt.Statements)
            {
                EmitStatement(s);
            }

            st.ExitScope();
        }

        void EmitVariableDeclarationStatement(VariableStmt statement)
        {
            VariableSymbol symbol = st.GetVariable(statement.Name);

            Type type = TranslateType(symbol.Type);

            var lb = il.DeclareLocal(type);
            symbol.LocalIndex = lb.LocalIndex;

            EmitExpression(statement.Expression);
            il.Emit(OpCodes.Stloc, symbol.LocalIndex);
        }

        void EmitReturnStatement(ReturnStmt returnStmt)
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

        void EmitIfStatement(IfStmt ifStmt)
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

        void EmitWhileStatement(WhileStmt whileStmt)
        {
            var loopStartLabel = il.DefineLabel();
            var loopEndLabel = il.DefineLabel();

            loopStart.Push(loopStartLabel);
            loopEnd.Push(loopEndLabel);

            il.MarkLabel(loopStartLabel);
            EmitExpression(whileStmt.Expression);

            il.Emit(OpCodes.Brfalse_S, loopEndLabel);

            EmitStatement(whileStmt.Statement);

            il.Emit(OpCodes.Br_S, loopStartLabel);

            il.MarkLabel(loopEndLabel);

            loopStart.Pop();
            loopEnd.Pop();
        }

        void EmitJumpStatement(JumpStmt jumpStatement)
        {
            if (jumpStatement.Token.Type == TokenType.Break)
            {
                il.Emit(OpCodes.Br_S, loopEnd.Last());
            }
            else if (jumpStatement.Token.Type == TokenType.Continue)
            {
                il.Emit(OpCodes.Br_S, loopStart.Last());
            }
        }

        TokenType EmitExpression(IExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    {
                        var expr = (LiteralExpr)expression;
                        return EmitPrimitive(expr.Token.Type, expr.Token.Value);
                    }
                case ExpressionType.Variable:
                    {
                        var expr = (VariableExpr)expression;

                        Symbol? symbol = st.GetSymbol(expr.Name);

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
                            return EmitPrimitive(constant.Type, constant.Value);
                        }

                        var parameters = functionParameters.Pop();
                        FunctionSymbol? function = st.GetFunction(expr.Name, parameters);
                        if (function is not null)
                        {
                            il.EmitCall(OpCodes.Call, combinedMethods[function], []);

                            return function.Type;
                        }

                        return TokenType.None;
                    }
                case ExpressionType.Assignment:
                    {
                        var expr = (AssignmentExpr)expression;

                        VariableSymbol? variable = st.GetVariable(expr.Name);

                        if (variable is not null)
                        {
                            EmitExpression(expr.Right);
                            il.Emit(OpCodes.Stloc, variable.LocalIndex);

                            return variable.Type;
                        }

                        return TokenType.None;
                    }
                case ExpressionType.Call:
                    {
                        var expr = (CallExpr)expression;

                        TokenType[] parameters = new TokenType[expr.Args.Count];
                        int i = 0;
                        foreach (var arg in expr.Args)
                        {
                            TokenType type = EmitExpression(arg);
                            parameters[i++] = type;
                        }

                        functionParameters.Push(parameters);

                        return EmitExpression(expr.Callee);
                    }
                case ExpressionType.Unary:
                    {
                        var expr = (UnaryExpr)expression;
                        var type = EmitExpression(expr.Expression);

                        switch (expr.Operator.Type)
                        {
                            case TokenType.Not when type == TokenType.Bool:
                                il.Emit(OpCodes.Ldc_I4, 0);
                                il.Emit(OpCodes.Ceq);
                                return TokenType.Bool;
                            case TokenType.Minus when type == TokenType.Int || type == TokenType.Real:
                                il.Emit(OpCodes.Neg);
                                return type;
                        }

                        break;
                    }
                case ExpressionType.Binary:
                    {
                        var expr = (BinaryExpr)expression;
                        var typeLeft = EmitExpression(expr.Left);
                        var typeRight = EmitExpression(expr.Right);

                        if (typeLeft != typeRight)
                        {
                            ParseError error = new(expr.Operator, appName,
                                $"Operator {expr.Operator.Type} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                            throw error.Exception();
                        }

                        switch (expr.Operator.Type)
                        {
                            case TokenType.Equal:
                                il.Emit(OpCodes.Ceq);
                                return TokenType.Bool;
                            case TokenType.NotEqual:
                                il.Emit(OpCodes.Ceq);
                                il.Emit(OpCodes.Ldc_I4, 0);
                                il.Emit(OpCodes.Ceq);
                                return TokenType.Bool;
                            case TokenType.Greater when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Cgt);
                                return TokenType.Bool;
                            case TokenType.GreaterEqual when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Clt);
                                il.Emit(OpCodes.Ldc_I4, 0);
                                il.Emit(OpCodes.Ceq);
                                return TokenType.Bool;
                            case TokenType.Less when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Clt);
                                return TokenType.Bool;
                            case TokenType.LessEqual when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Cgt);
                                il.Emit(OpCodes.Ldc_I4, 0);
                                il.Emit(OpCodes.Ceq);
                                return TokenType.Bool;
                            case TokenType.Plus when typeLeft == TokenType.String && typeRight == TokenType.String:
                                il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]), []);
                                return TokenType.String;
                            case TokenType.Plus when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Add);
                                return typeLeft;
                            case TokenType.Minus when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Sub);
                                return typeLeft;
                            case TokenType.Slash when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Div);
                                return typeLeft;
                            case TokenType.Star when typeLeft == TokenType.Int || typeLeft == TokenType.Real:
                                il.Emit(OpCodes.Mul);
                                return typeLeft;
                        }

                        break;
                    }

                case ExpressionType.Logical:
                    {
                        var expr = (LogicalExpr)expression;

                        if ((expr.Left.Type == ExpressionType.Literal || expr.Left.Type == ExpressionType.Variable)
                            && (expr.Right.Type == ExpressionType.Literal || expr.Right.Type == ExpressionType.Variable))
                        {
                            TokenType typeLeft = EmitExpression(expr.Left);
                            TokenType typeRight = EmitExpression(expr.Right);

                            if (typeLeft != TokenType.Bool && typeLeft != typeRight)
                            {
                                ParseError error = new(expr.Operator, appName,
                                    $"Operator {expr.Operator.Type} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                                throw error.Exception();
                            }

                            if (expr.Operator.Type == TokenType.And)
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
                            TokenType typeLeft = EmitExpression(expr.Left);

                            if (expr.Operator.Type == TokenType.And)
                            {
                                Label leftTrue = il.DefineLabel();
                                il.Emit(OpCodes.Brtrue_S, leftTrue);
                                il.Emit(OpCodes.Ldc_I4, 0);
                                Label endLabel = il.DefineLabel();
                                il.Emit(OpCodes.Br_S, endLabel);

                                il.MarkLabel(leftTrue);
                                TokenType typeRight = EmitExpression(expr.Right);

                                if (typeLeft != TokenType.Bool && typeLeft != typeRight)
                                {
                                    ParseError error = new(expr.Operator, appName,
                                        $"Operator {expr.Operator.Type} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                                    throw error.Exception();
                                }

                                il.MarkLabel(endLabel);
                            }
                            else
                            {
                                Label leftTrue = il.DefineLabel();
                                il.Emit(OpCodes.Brtrue_S, leftTrue);
                                TokenType typeRight = EmitExpression(expr.Right);

                                if (typeLeft != TokenType.Bool && typeLeft != typeRight)
                                {
                                    ParseError error = new(expr.Operator, appName,
                                        $"Operator {expr.Operator.Type} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                                    throw error.Exception();
                                }
                                Label endLabel = il.DefineLabel();
                                il.Emit(OpCodes.Br_S, endLabel);

                                il.MarkLabel(leftTrue);
                                il.Emit(OpCodes.Ldc_I4, 1);

                                il.MarkLabel(endLabel);
                            }
                        }

                        return TokenType.Bool;
                    }
            }

            return TokenType.None;
        }

        TokenType EmitPrimitive(TokenType type, object value)
        {
            switch (type)
            {
                case TokenType.LiteralString: il.Emit(OpCodes.Ldstr, (string)value); return TokenType.String;
                case TokenType.LiteralInt: il.Emit(OpCodes.Ldc_I4, (int)value); return TokenType.Int;
                case TokenType.LiteralReal: il.Emit(OpCodes.Ldc_R8, (double)value); return TokenType.Real;
                case TokenType.LiteralTrue: il.Emit(OpCodes.Ldc_I4, 1); return TokenType.Bool;
                case TokenType.LiteralFalse: il.Emit(OpCodes.Ldc_I4, 0); return TokenType.Bool;
                default: il.Emit(OpCodes.Ldnull); return TokenType.None;
            }
        }

        static Type TranslateType(TokenType type)
        {
            return type switch
            {
                TokenType.Bool => typeof(bool),
                TokenType.LiteralTrue => typeof(bool),
                TokenType.LiteralFalse => typeof(bool),
                TokenType.Int => typeof(int),
                TokenType.Real => typeof(double),
                TokenType.String => typeof(string),
                _ => typeof(void),
            };
        }

        static Type[] TranslateTypes(TokenType[] types)
        {
            List<Type> result = new(types.Length);

            foreach (var type in types)
            {
                result.Add(TranslateType(type));
            }

            return [.. result];
        }

        static int CountReturnStatements(IStatement statement)
        {
            if (statement.Type == StatementType.Return)
            {
                return 1;
            }

            int count = 0;
            if (statement is FunctionStmt fStmt)
            {
                count += CountReturnStatements(fStmt.Body);
            }
            else if (statement is BlockStmt blockStmt)
            {
                foreach (var stmt in blockStmt.Statements)
                {
                    count += CountReturnStatements(stmt);
                }
            }
            else if (statement is IfStmt ifStmt)
            {
                count += CountReturnStatements(ifStmt.ThenStatement);

                if (ifStmt.ElseStatement is not null)
                {
                    count += CountReturnStatements(ifStmt.ElseStatement);
                }
            }
            else if (statement is WhileStmt whileStmt)
            {
                count += CountReturnStatements(whileStmt.Statement);
            }

            return count;
        }
    }
}
