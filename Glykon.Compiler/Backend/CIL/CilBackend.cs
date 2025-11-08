using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public sealed class CilBackend
{
    private readonly string appName;
    private readonly IdentifierInterner interner;
    private readonly AssemblyName asmName;
    private readonly PersistedAssemblyBuilder ab;

    private int metadataToken;
    
    public CilBackend(string appName, IdentifierInterner interner)
    {
        this.appName = appName;
        this.interner = interner;
        asmName = new AssemblyName(appName);
        ab = new PersistedAssemblyBuilder(asmName, typeof(object).Assembly);
    }

    public Assembly Emit(IRTree irTree, SymbolTable symbolTable, TypeSystem typeSystem, bool saveToDisk = false)
    {
        var mob = ab.DefineDynamicModule(asmName.Name!);
        
        var typeEmitter = new CilTypeEmitter(irTree, symbolTable, typeSystem, interner, appName);
        var definedMethods = typeEmitter.EmitAssembly(mob);

        int mainId = interner.Intern("main");
        var mains = definedMethods
            .Where(t => t.Symbol.QualifiedNameId == mainId)
            .ToList();

        if (mains.Count == 0)
            throw new InvalidOperationException("No entry point found ('main').");

        var mainInfo = mains.Count == 1
            ? mains[0]
            : mains.FirstOrDefault(m => m.Method.GetParameters().Length == 0)
              ?? throw new InvalidOperationException("Multiple 'main' overloads; define a parameterless main.");

        metadataToken = mainInfo.Method.MetadataToken;

        if (saveToDisk)
        {
            Save();
        }

        return GetAssembly();
    }

    private void Save()
    {
        MetadataBuilder metadataBuilder = ab.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData);
        PEHeaderBuilder peHeaderBuilder = new(imageCharacteristics: Characteristics.ExecutableImage);

        ManagedPEBuilder peBuilder = new(
            header: peHeaderBuilder,
            metadataRootBuilder: new MetadataRootBuilder(metadataBuilder),
            ilStream: ilStream,
            mappedFieldData: fieldData,
            entryPoint: MetadataTokens.MethodDefinitionHandle(metadataToken));

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

    private Assembly GetAssembly()
    {
        using var stream = new MemoryStream();
        ab.Save(stream);
        stream.Seek(0, SeekOrigin.Begin);
        Assembly assembly = AssemblyLoadContext.Default.LoadFromStream(stream);
        return assembly;
    }
}