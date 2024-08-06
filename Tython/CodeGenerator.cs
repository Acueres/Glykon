using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using Tython.Model;
using System.Reflection.Emit;
using System.Runtime.Loader;
using Tython.Enum;

namespace Tython
{
    public class CodeGenerator
    {
        readonly string appname;
        readonly Statement[] statements;
        readonly SymbolTable symbolTable;

        readonly PersistedAssemblyBuilder ab;
        readonly MethodBuilder main;
        readonly ILGenerator il;

        public CodeGenerator(Statement[] statements, SymbolTable symbolTable, string appname)
        {
            this.appname = appname;
            this.statements = statements;
            this.symbolTable = symbolTable;

            EmitMain(out ab, out main, out il);
        }

        void EmitMain(out PersistedAssemblyBuilder ab, out MethodBuilder main, out ILGenerator il)
        {
            ab = new(new AssemblyName(appname), typeof(object).Assembly);
            ModuleBuilder mob = ab.DefineDynamicModule(appname);
            TypeBuilder tb = mob.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            main = tb.DefineMethod("Main", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static);
            il = main.GetILGenerator();

            foreach (Statement statement in statements)
            {
                if (statement.Type == StatementType.Print)
                {
                    EmitPrintStatement(statement);
                }
                else if (statement.Type == StatementType.Variable)
                {
                    EmitVariableDeclarationStatement(statement);
                }
            }

            il.Emit(OpCodes.Ret);

            tb.CreateType();
        }

        void EmitPrintStatement(Statement statement)
        {
            EmitExpression(statement.Expression);
            il.EmitCall(OpCodes.Call, typeof(Console).GetMethod("WriteLine", [typeof(string)]), []);
        }

        void EmitVariableDeclarationStatement(Statement statement)
        {
            il.DeclareLocal(typeof(string));
            EmitExpression(statement.Expression);
            il.Emit(OpCodes.Stloc, symbolTable.Get(statement.Token.Value as string));
        }

        void EmitExpression(IExpression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    {
                        var expr = (LiteralExpr)expression;
                        il.Emit(OpCodes.Ldstr, expr.Token.Value.ToString());
                        break;
                    }
                case ExpressionType.Variable:
                    {
                        var expr = (VariableExpr)expression;
                        il.Emit(OpCodes.Ldloc, symbolTable.Get(expr.Name));
                        break;
                    }
                case ExpressionType.Binary:
                    {
                        var expr = (BinaryExpr)expression;
                        EmitExpression(expr.Left);
                        EmitExpression(expr.Right);

                        switch (expr.Operator.Type)
                        {
                            case TokenType.Plus:
                                il.EmitCall(OpCodes.Call, typeof(string).GetMethod("Concat", [typeof(string), typeof(string)]), []);
                                break;
                        }

                        break;
                    }
            }
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

            using var fileStream = new FileStream($"{appname}.exe", FileMode.Create, FileAccess.Write);
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

            using StreamWriter outputFile = new($"{appname}.runtimeconfig.json");
            {
                outputFile.WriteLine(runtimeconfig);
            }
        }
    }
}
