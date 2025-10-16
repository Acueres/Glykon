using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;
using Glykon.Compiler.Semantics.Binding.BoundStatements;

namespace Glykon.Compiler.Emitter;

public class TypeEmitter
{
    readonly string appName;
    readonly BoundTree boundTree;
    readonly SymbolTable symbolTable;
    readonly IdentifierInterner interner;

    readonly PersistedAssemblyBuilder ab;
    MethodBuilder main;

    public TypeEmitter(BoundTree boundTree, SymbolTable symbolTable, IdentifierInterner interner, string appname)
    {
        appName = appname;
        this.boundTree = boundTree;
        this.symbolTable = symbolTable;
        this.interner = interner;

        ab = new(new AssemblyName(appName), typeof(object).Assembly);
    }

    public void EmitAssembly()
    {
        symbolTable.ResetScope();

        ModuleBuilder mob = ab.DefineDynamicModule(appName);
        TypeBuilder tb = mob.DefineType("Program", TypeAttributes.Public | TypeAttributes.Class);

        List<MethodEmitter> methodGenerators = [];
        Dictionary<FunctionSymbol, MethodInfo> methods = LoadStdLibrary();

        foreach (var stmt in boundTree)
        {
            if (stmt is BoundFunctionDeclaration f)
            {
                MethodEmitter mg = new(f, interner, tb, appName);
                methodGenerators.Add(mg);
                methods.Add(f.Signature, mg.GetMethodBuilder());

                string name = interner[f.Signature.QualifiedNameId];
                if (name == "main")
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

        const string runtimeConfig = @"{
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
        outputFile.WriteLine(runtimeConfig);
    }

    Dictionary<FunctionSymbol, MethodInfo> LoadStdLibrary()
    {
        Dictionary<FunctionSymbol, MethodInfo> stdFunctions = [];

        var console = typeof(Console);

        stdFunctions.Add(symbolTable.GetFunction("println", [TokenKind.String]),
            console.GetMethod("WriteLine", [typeof(string)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [TokenKind.Int]),
            console.GetMethod("WriteLine", [typeof(int)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [TokenKind.Real]),
            console.GetMethod("WriteLine", [typeof(double)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [TokenKind.Bool]),
            console.GetMethod("WriteLine", [typeof(bool)]));

        return stdFunctions;
    }
}

