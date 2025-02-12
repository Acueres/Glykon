using System.Reflection;
using System.Reflection.Emit;
using TythonCompiler.Tokenization;
using TythonCompiler.SemanticAnalysis;
using TythonCompiler.Diagnostics.Errors;
using TythonCompiler.SemanticAnalysis.Symbols;
using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Syntax.Statements;

namespace TythonCompiler.CodeGeneration
{
    internal class MethodGenerator
    {
        readonly MethodBuilder mb;
        readonly ILGenerator il;

        readonly FunctionStmt fStmt;
        readonly SymbolTable st;
        readonly string appName;

        Dictionary<string, MethodBuilder> methods = [];

        readonly Label? returnLabel;
        readonly LocalBuilder? returnLocal; 

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
            bool multipleReturns = CountReturnStatements(stmt) > 1;

            if (multipleReturns || (stmt.ReturnType == TokenType.None && n > 0))
            {
                returnLabel = il.DefineLabel();
            }

            if (stmt.ReturnType != TokenType.None && multipleReturns)
            {
                returnLocal = il.DeclareLocal(TranslateType(stmt.ReturnType));
            }
        }

        public MethodBuilder GetMethodBuilder() => mb;

        public void EmitMethod(Dictionary<string, MethodBuilder> methods)
        {
            this.methods = methods;

            st.EnterScope(fStmt.ScopeIndex);

            foreach (IStatement stmt in fStmt.Body)
            {
                EmitStatement(stmt);
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
                case StatementType.Print:
                    EmitPrintStatement((PrintStmt)statement);
                    break;
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

        void EmitPrintStatement(PrintStmt statement)
        {
            var type = EmitExpression(statement.Expression);
            MethodInfo method = type switch
            {
                TokenType.String => typeof(Console).GetMethod("WriteLine", [typeof(string)]),
                TokenType.Int => typeof(Console).GetMethod("WriteLine", [typeof(int)]),
                TokenType.Real => typeof(Console).GetMethod("WriteLine", [typeof(double)]),
                TokenType.Bool => typeof(Console).GetMethod("WriteLine", [typeof(bool)]),
                _ => typeof(Console).GetMethod("WriteLine", [typeof(object)]),
            };
            il.EmitCall(OpCodes.Call, method, []);
        }

        void EmitVariableDeclarationStatement(VariableStmt statement)
        {
            VariableSymbol symbol = st.GetVariable(statement.Name);

            Type type = TranslateType(symbol.Type);

            var lb = il.DeclareLocal(type);
            symbol.LocalIndex = lb.LocalIndex;

            EmitExpression(statement.Expression);
            il.Emit(OpCodes.Stloc, symbol.LocalIndex);

            st.Initializevariable(statement.Name);
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

                EmitStatement(ifStmt.Statement);

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

                EmitStatement(ifStmt.Statement);

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

                        ParameterSymbol? parameter = st.GetParameter(expr.Name);
                        if (parameter is not null)
                        {
                            il.Emit(OpCodes.Ldarg, parameter.Index);
                            return parameter.Type == TokenType.True || parameter.Type == TokenType.False ? TokenType.Bool : parameter.Type;
                        }

                        VariableSymbol? variable = st.GetInitializedVariable(expr.Name);
                        if (variable is not null)
                        {
                            il.Emit(OpCodes.Ldloc, variable.LocalIndex);

                            return variable.Type == TokenType.True || variable.Type == TokenType.False ? TokenType.Bool : variable.Type;
                        }

                        FunctionSymbol? function = st.GetFunction(expr.Name);
                        if (function is not null)
                        {
                            il.EmitCall(OpCodes.Call, methods[expr.Name], []);

                            return function.ReturnType;
                        }

                        ConstantSymbol? constant = st.GetConstant(expr.Name);
                        if (constant is not null)
                        {
                            return EmitPrimitive(constant.Type, constant.Value);
                        }

                        return TokenType.Null;
                    }
                case ExpressionType.Assignment:
                    {
                        var expr = (AssignmentExpr)expression;

                        ParameterSymbol? parameter = st.GetParameter(expr.Name);
                        if (parameter is not null)
                        {
                            il.Emit(OpCodes.Starg, parameter.Index);
                            return parameter.Type == TokenType.True || parameter.Type == TokenType.False ? TokenType.Bool : parameter.Type;
                        }
                        else
                        {
                            VariableSymbol symbol = st.GetInitializedVariable(expr.Name);

                            EmitExpression(expr.Right);
                            il.Emit(OpCodes.Stloc, symbol.LocalIndex);

                            return symbol.Type;
                        }
                    }
                case ExpressionType.Call:
                    {
                        var expr = (CallExpr)expression;

                        foreach (var arg in expr.Args)
                        {
                            EmitExpression(arg);
                        }

                        return EmitExpression(expr.Callee);
                    }
                case ExpressionType.Unary:
                    {
                        var expr = (UnaryExpr)expression;
                        var type = EmitExpression(expr.Expr);

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
                case TokenType.String: il.Emit(OpCodes.Ldstr, (string)value); return TokenType.String;
                case TokenType.Int: il.Emit(OpCodes.Ldc_I4, (int)value); return TokenType.Int;
                case TokenType.Real: il.Emit(OpCodes.Ldc_R8, (double)value); return TokenType.Real;
                case TokenType.True: il.Emit(OpCodes.Ldc_I4, 1); return TokenType.Bool;
                case TokenType.False: il.Emit(OpCodes.Ldc_I4, 0); return TokenType.Bool;
                default: il.Emit(OpCodes.Ldnull); return TokenType.None;
            }
        }

        static Type TranslateType(TokenType type)
        {
            return type switch
            {
                TokenType.Bool => typeof(bool),
                TokenType.True => typeof(bool),
                TokenType.False => typeof(bool),
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
            if (statement is ReturnStmt)
            {
                return 1;
            }

            int count = 0;
            if (statement is FunctionStmt fStmt)
            {
                foreach (var stmt in fStmt.Body)
                {
                    count += CountReturnStatements(stmt);
                }
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
                count += CountReturnStatements(ifStmt.Statement);
            }
            else if (statement is WhileStmt whileStmt)
            {
                count += CountReturnStatements(whileStmt.Statement);
            }

            return count;
        }
    }
}
