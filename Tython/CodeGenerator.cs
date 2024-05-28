using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using Tython.Model;
using System.Reflection.Emit;
using System.Runtime.Loader;
using System.Globalization;

namespace Tython
{
    public class CodeGenerator
    {
        readonly Statement[] statements;
        readonly string appname;

        readonly PersistedAssemblyBuilder ab;
        readonly MethodBuilder main;
        readonly ILGenerator il;

        public CodeGenerator(Statement[] statements, string appname)
        {
            this.statements = statements;
            this.appname = appname;

            GenerateMain(out ab, out main, out il);
        }

        void GenerateMain(out PersistedAssemblyBuilder ab, out MethodBuilder main, out ILGenerator il)
        {
            ab = new(new AssemblyName(appname), typeof(object).Assembly);
            ModuleBuilder mob = ab.DefineDynamicModule(appname);
            TypeBuilder tb = mob.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);
            main = tb.DefineMethod("Main", MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static);
            il = main.GetILGenerator();

            foreach (Statement statement in statements)
            {
                if (statement.Token.Value == "print")
                {
                    GeneratePrintStatement(statement);
                }
            }

            il.Emit(OpCodes.Ret);

            tb.CreateType();
        }

        void GeneratePrintStatement(Statement statement)
        {
            object value = EvaluateExpression(statement.Expression);
            il.EmitWriteLine(value.ToString());
        }

        object EvaluateExpression(Expression expression)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal:
                    return expression.Token.Type switch
                    {
                        TokenType.String => expression.Token.Value,
                        TokenType.Int => long.Parse(expression.Token.Value),
                        TokenType.Real => double.Parse(expression.Token.Value, CultureInfo.InvariantCulture),
                        TokenType.Keyword => bool.Parse(expression.Token.Value),
                        _ => throw new Exception("Token not a literal"),
                    };
                case ExpressionType.Grouping:
                    return EvaluateExpression(expression.Primary);
                case ExpressionType.Unary:
                    {
                        object primary = EvaluateExpression(expression.Primary);

                        switch (expression.Token.Value)
                        {
                            case "-":
                                {
                                    if (primary is long l) return -l;
                                    else if (primary is double d) return -d;
                                    else throw new Exception($"Operator - not defined for {primary}");

                                }
                            case "not":
                                {
                                    if (primary is bool b) return !b;
                                    else throw new Exception($"Operator not not defined for {primary}");
                                }
                            default:
                                throw new Exception($"Operator {expression.Token.Value} is not unary");
                        }
                    }
                case ExpressionType.Binary:
                    {
                        object primary = EvaluateExpression(expression.Primary);
                        object secondary = EvaluateExpression(expression.Secondary);

                        switch (expression.Token.Value) {
                            case "-":
                                {
                                    if (primary is long && secondary is long) return (long)primary - (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary - (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary - (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary - (long)secondary;
                                    else throw new Exception($"Operator - not defined for {primary}, {secondary}");
                                }
                            case "+":
                                {
                                    if (primary is string && secondary is string) return (string)primary + (string)secondary;

                                    if (primary is long && secondary is long) return (long)primary + (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary + (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary + (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary + (long)secondary;
                                    else throw new Exception($"Operator + not defined for {primary}, {secondary}");
                                }
                            case "/":
                                {
                                    if ((secondary is double && (double)secondary == 0)
                                        || (secondary is long && (long)secondary == 0)) throw new Exception("Division by zero");

                                    if (primary is long && secondary is long) return (long)primary / (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary / (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary / (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary / (long)secondary;
                                    else throw new Exception($"Operator / not defined for {primary}, {secondary}");
                                }
                            case "*":
                                if (primary is long && secondary is long) return (long)primary * (long)secondary;
                                else if (primary is double && secondary is double) return (double)primary * (double)secondary;
                                else if (primary is long && secondary is double) return (long)primary * (double)secondary;
                                else if (primary is double && secondary is long) return (double)primary * (long)secondary;
                                else throw new Exception($"Operator * not defined for {primary}, {secondary}");
                            case ">":
                                {
                                    if (primary is long && secondary is long) return (long)primary > (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary > (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary > (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary > (long)secondary;
                                    else throw new Exception($"Operator > not defined for {primary}, {secondary}");
                                }
                            case ">=":
                                {
                                    if (primary is long && secondary is long) return (long)primary >= (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary >= (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary >= (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary >= (long)secondary;
                                    else throw new Exception($"Operator >= not defined for {primary}, {secondary}");
                                }
                            case "<":
                                {
                                    if (primary is long && secondary is long) return (long)primary < (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary < (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary < (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary < (long)secondary;
                                    else throw new Exception($"Operator < not defined for {primary}, {secondary}");
                                }
                            case "<=":
                                {
                                    if (primary is long && secondary is long) return (long)primary <= (long)secondary;
                                    else if (primary is double && secondary is double) return (double)primary <= (double)secondary;
                                    else if (primary is long && secondary is double) return (long)primary <= (double)secondary;
                                    else if (primary is double && secondary is long) return (double)primary <= (long)secondary;
                                    else throw new Exception($"Operator <= not defined for {primary}, {secondary}");
                                }
                            case "==":
                                {
                                    return primary.Equals(secondary);
                                }
                            case "!=":
                                {
                                    return !primary.Equals(secondary);
                                }
                            default:
                                throw new Exception($"Operator {expression.Token.Value} is not binary");
                        }
                    }
            }

            return null;
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
