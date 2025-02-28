using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;
using TythonCompiler.SemanticAnalysis;
using TythonCompiler.Syntax.Statements;
using TythonCompiler.Tokenization;

namespace TythonCompiler.CodeGeneration;

public class TypeGenerator
{
    readonly string appName;
    readonly IStatement[] statements;
    readonly SymbolTable symbolTable;

    readonly PersistedAssemblyBuilder ab;
    MethodBuilder main;

    public TypeGenerator(IStatement[] statements, SymbolTable symbolTable, string appname)
    {
        appName = appname;
        this.statements = statements;
        this.symbolTable = symbolTable;

        ab = new(new AssemblyName(appName), typeof(object).Assembly);
    }

    public void GenerateAssembly()
    {
        ModuleBuilder mob = ab.DefineDynamicModule(appName);
        TypeBuilder tb = mob.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);

        List<MethodGenerator> methodGenerators = [];
        Dictionary<FunctionSignature, MethodInfo> methods = GetStdLibrary();

        symbolTable.ResetScope();

        foreach (var stmt in statements)
        {
            if (stmt is FunctionStmt f)
            {
                MethodGenerator mg = new(f, symbolTable, tb, appName);
                methodGenerators.Add(mg);
                methods.Add(f.Signature, mg.GetMethodBuilder());

                if (f.Name == "main")
                {
                    main = mg.GetMethodBuilder();
                }
            }
        }

        foreach (var mg in methodGenerators)
        {
            mg.EmitMethod(methods);
        }

        tb.CreateType();
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
        ""version"": ""9.0.0""
      }
    }
  }
";

        using StreamWriter outputFile = new($"{appName}.runtimeconfig.json");
        outputFile.WriteLine(runtimeconfig);
    }

    Dictionary<FunctionSignature, MethodInfo> GetStdLibrary()
    {
        Dictionary<FunctionSignature, MethodInfo> nativeFunctions = [];

        var console = typeof(Console);

        nativeFunctions.Add(symbolTable.RegisterFunction("println", TokenType.None, [TokenType.String]),
            console.GetMethod("WriteLine", [typeof(string)]));

        nativeFunctions.Add(symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Int]),
            console.GetMethod("WriteLine", [typeof(int)]));

        nativeFunctions.Add(symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Real]),
            console.GetMethod("WriteLine", [typeof(double)]));

        nativeFunctions.Add(symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Bool]),
            console.GetMethod("WriteLine", [typeof(bool)]));

        return nativeFunctions;
    }
}

