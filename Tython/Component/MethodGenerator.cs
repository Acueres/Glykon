using System.Reflection;
using System.Reflection.Emit;
using Tython.Model;

namespace Tython.Component
{
    internal class MethodGenerator
    {
        readonly MethodBuilder mb;
        readonly ILGenerator il;

        readonly FunctionStmt fStmt;
        readonly SymbolTable st;
        readonly string appName;

        readonly Dictionary<string, (int, TokenType)> parameters = [];

        Dictionary<string, MethodBuilder> methods = [];
        Label loopStart;
        Label loopEnd;

        public MethodGenerator(FunctionStmt stmt, SymbolTable symbolTable, TypeBuilder typeBuilder, string appName)
        {
            var ts = TranslateTypes([.. stmt.Parameters.Select(p => p.Type)]);

            mb = typeBuilder.DefineMethod(stmt.Name,
                MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
                typeof(void), TranslateTypes([.. stmt.Parameters.Select(p => p.Type)]));

            for (int i = 0; i < stmt.Parameters.Count; i++)
            {
                mb.DefineParameter(i + 1, ParameterAttributes.None, stmt.Parameters[i].Name);
                parameters.Add(stmt.Parameters[i].Name, (i, stmt.Parameters[i].Type));
            }

            il = mb.GetILGenerator();

            fStmt = stmt;
            st = symbolTable;
            this.appName = appName;
        }

        public MethodBuilder GetMethodBuilder() => mb;

        public void EmitMethod(Dictionary<string, MethodBuilder> methods)
        {
            this.methods = methods;

            EmitStatement(fStmt.Body);

            il.Emit(OpCodes.Ret);
        }

        void EmitStatement(IStatement statement)
        {
            if (statement.Type == StatementType.Print)
            {
                EmitPrintStatement((PrintStmt)statement);
            }
            else if (statement.Type == StatementType.Variable)
            {
                EmitVariableDeclarationStatement((VariableStmt)statement);
            }
            else if (statement.Type == StatementType.If)
            {
                EmitIfStatement((IfStmt)statement);
            }
            else if (statement.Type == StatementType.While)
            {
                EmitWhileStatement((WhileStmt)statement);
            }
            else if (statement.Type == StatementType.Block)
            {
                EmitBlockStatement((BlockStmt)statement);
            }
            else if (statement is JumpStmt jumpStmt)
            {
                EmitJumpStatement(jumpStmt);
            }
            else
            {
                EmitExpression(statement.Expression);
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

            Type type = symbol.Type switch
            {
                TokenType.String => typeof(string),
                TokenType.Int => typeof(int),
                TokenType.Real => typeof(double),
                TokenType.Bool => typeof(bool),
                TokenType.True => typeof(bool),
                TokenType.False => typeof(bool),
                _ => typeof(object),
            };

            il.DeclareLocal(type);
            EmitExpression(statement.Expression);
            il.Emit(OpCodes.Stloc, symbol.LocalIndex);

            st.Initializevariable(statement.Name);
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
            loopStart = il.DefineLabel();
            loopEnd = il.DefineLabel();

            il.MarkLabel(loopStart);
            EmitExpression(whileStmt.Expression);

            il.Emit(OpCodes.Brfalse_S, loopEnd);

            EmitStatement(whileStmt.Statement);

            il.Emit(OpCodes.Br_S, loopStart);

            il.MarkLabel(loopEnd);
        }

        void EmitJumpStatement(JumpStmt jumpStatement)
        {
            if (jumpStatement.Token.Type == TokenType.Break)
            {
                il.Emit(OpCodes.Br_S, loopEnd);
            }
            else if (jumpStatement.Token.Type == TokenType.Continue)
            {
                il.Emit(OpCodes.Br_S, loopStart);
            }
        }

        TokenType EmitExpression(IExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    {
                        var expr = (LiteralExpr)expression;
                        switch (expr.Token.Type)
                        {
                            case TokenType.String: il.Emit(OpCodes.Ldstr, expr.Token.Value.ToString()); return TokenType.String;
                            case TokenType.Int: il.Emit(OpCodes.Ldc_I4, (int)expr.Token.Value); return TokenType.Int;
                            case TokenType.Real: il.Emit(OpCodes.Ldc_R8, (double)expr.Token.Value); return TokenType.Real;
                            case TokenType.True: il.Emit(OpCodes.Ldc_I4, 1); return TokenType.Bool;
                            case TokenType.False: il.Emit(OpCodes.Ldc_I4, 0); return TokenType.Bool;
                        }
                        return TokenType.None;
                    }
                case ExpressionType.Variable:
                    {
                        var expr = (VariableExpr)expression;

                        if (parameters.TryGetValue(expr.Name, out var p))
                        {
                            il.Emit(OpCodes.Ldarg, p.Item1);
                            return p.Item2 == TokenType.True || p.Item2 == TokenType.False ? TokenType.Bool : p.Item2;
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
                            switch (constant.Type)
                            {
                                case TokenType.String: il.Emit(OpCodes.Ldstr, (string)constant.Value); return TokenType.String;
                                case TokenType.Int: il.Emit(OpCodes.Ldc_I4, (int)constant.Value); return TokenType.Int;
                                case TokenType.Real: il.Emit(OpCodes.Ldc_R8, (double)constant.Value); return TokenType.Real;
                                case TokenType.True: il.Emit(OpCodes.Ldc_I4, 1); return TokenType.Bool;
                                case TokenType.False: il.Emit(OpCodes.Ldc_I4, 0); return TokenType.Bool;
                            }

                            return constant.Type;
                        }

                        return TokenType.Null;
                    }
                case ExpressionType.Assignment:
                    {
                        var expr = (AssignmentExpr)expression;

                        if (parameters.TryGetValue(expr.Name, out var p))
                        {
                            il.Emit(OpCodes.Starg, p.Item1);
                            return p.Item2 == TokenType.True || p.Item2 == TokenType.False ? TokenType.Bool : p.Item2;
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

                        EmitExpression(expr.Callee);

                        return TokenType.None;
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

        static Type[] TranslateTypes(TokenType[] types)
        {
            List<Type> result = new(types.Length);

            foreach (var type in types)
            {
                switch (type)
                {
                    case TokenType.Bool: result.Add(typeof(bool)); break;
                    case TokenType.True: result.Add(typeof(bool)); break;
                    case TokenType.False: result.Add(typeof(bool)); break;
                    case TokenType.Int: result.Add(typeof(int)); break;
                    case TokenType.Real: result.Add(typeof(double)); break;
                    case TokenType.String: result.Add(typeof(string)); break;
                }
            }

            return [.. result];
        }
    }
}
