using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using Tython.Model;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Tython.Enum;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tython
{
    public class CodeGenerator
    {
        readonly string appName;
        readonly IStatement[] statements;
        readonly SymbolTable symbolTable;

        readonly PersistedAssemblyBuilder ab;
        readonly MethodBuilder main;
        readonly ILGenerator il;

        public CodeGenerator(IStatement[] statements, SymbolTable symbolTable, string appname)
        {
            this.appName = appname;
            this.statements = statements;
            this.symbolTable = symbolTable;

            EmitMain(out ab, out main, out il);
        }

        void EmitMain(out PersistedAssemblyBuilder ab, out MethodBuilder main, out ILGenerator il)
        {
            ab = new(new AssemblyName(appName), typeof(object).Assembly);
            ModuleBuilder mob = ab.DefineDynamicModule(appName);
            TypeBuilder tb = mob.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            main = tb.DefineMethod("Main", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static);
            il = main.GetILGenerator();

            foreach (var statement in statements)
            {
                if (statement.Type == StatementType.Print)
                {
                    EmitPrintStatement((PrintStmt)statement);
                }
                else if (statement.Type == StatementType.Variable)
                {
                    EmitVariableDeclarationStatement((VariableStmt)statement);
                }
            }

            il.Emit(OpCodes.Ret);

            tb.CreateType();
        }

        void EmitPrintStatement(PrintStmt statement)
        {
            var type = EmitExpression(statement.Expression);
            MethodInfo method = type switch
            {
                TokenType.String => typeof(Console).GetMethod("WriteLine", [typeof(string)]),
                TokenType.Int => typeof(Console).GetMethod("WriteLine", [typeof(long)]),
                TokenType.Real => typeof(Console).GetMethod("WriteLine", [typeof(double)]),
                TokenType.Bool => typeof(Console).GetMethod("WriteLine", [typeof(bool)]),
                _ => typeof(Console).GetMethod("WriteLine", [typeof(object)]),
            };
            il.EmitCall(OpCodes.Call, method, []);
        }

        void EmitVariableDeclarationStatement(VariableStmt statement)
        {
            (int index, TokenType varType) = symbolTable.Get(statement.Name);

            Type type = varType switch
            {
                TokenType.String => typeof(string),
                TokenType.Int => typeof(long),
                TokenType.Real => typeof(double),
                TokenType.Bool => typeof(bool),
                _ => typeof(object),
            };

            il.DeclareLocal(type);
            EmitExpression(statement.Expression);
            il.Emit(OpCodes.Stloc, index);
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
                            case TokenType.Int: il.Emit(OpCodes.Ldc_I8, (long)expr.Token.Value); return TokenType.Int;
                            case TokenType.Real: il.Emit(OpCodes.Ldc_R8, (double)expr.Token.Value); return TokenType.Real;
                            case TokenType.True: il.Emit(OpCodes.Ldc_I4, 1); return TokenType.Bool;
                            case TokenType.False: il.Emit(OpCodes.Ldc_I4, 0); return TokenType.Bool;
                        }
                        break;
                    }
                case ExpressionType.Variable:
                    {
                        var expr = (VariableExpr)expression;
                        (int index, TokenType varType) = symbolTable.Get(expr.Name);
                        il.Emit(OpCodes.Ldloc, index);
                        return varType;
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
                            case TokenType.And when typeLeft == TokenType.Bool && typeLeft == TokenType.Bool:
                                il.Emit(OpCodes.And);
                                return TokenType.Bool;
                            case TokenType.Or when typeLeft == TokenType.Bool && typeLeft == TokenType.Bool:
                                il.Emit(OpCodes.Or);
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
            }

            return TokenType.None;
        }

        public Assembly GetAssembly()
        {
            using var stream = new MemoryStream();
            ab.Save(stream);
            stream.Seek(0, SeekOrigin.Begin);
            Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
            return assembly;
        }

        public void Save()
        {
            MetadataBuilder metadataBuilder = ab.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData);
            PEHeaderBuilder peHeaderBuilder = new(imageCharacteristics: Characteristics.ExecutableImage);

            ManagedPEBuilder peBuilder = new(
                            header: peHeaderBuilder,
                            metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
                            ilStream: ilStream,
                            mappedFieldData: fieldData,
                            entryPoint: MetadataTokens.MethodDefinitionHandle(main.MetadataToken));

            BlobBuilder peBlob = new();
            peBuilder.Serialize(peBlob);

            using var fileStream = new FileStream($"{appName}.exe", FileMode.Create, FileAccess.Write);
            peBlob.WriteContentTo(fileStream);

            const string runtimeconfig = @"{
    ""runtimeOptions"": {
      ""tfm"": ""net9.0"",
      ""framework"": {
        ""name"": ""Microsoft.NETCore.App"",
        ""version"": ""9.0.0-preview.3.24172.9""
      }
    }
  }
";

            using StreamWriter outputFile = new($"{appName}.runtimeconfig.json");
            {
                outputFile.WriteLine(runtimeconfig);
            }
        }
    }
}
