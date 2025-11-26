using System.Reflection.Metadata.Ecma335;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.Loader;

using Glykon.Compiler.Semantics.Analysis;
using Glykon.Compiler.Semantics.Binding;

namespace Glykon.Compiler.Backend.CIL;

public sealed class CilBackend(SemanticResult semanticResult, AssemblyName asmName)
{
    private readonly IdentifierInterner interner = semanticResult.Interner;
    private readonly PersistedAssemblyBuilder ab = new(asmName, typeof(object).Assembly);

    public EmittedImage EmitToImage()
    {
        var mob = ab.DefineDynamicModule(asmName.Name!);

        var typeEmitter = new CilCompilationUnitEmitter(semanticResult.Ir, semanticResult.SymbolTable,
            semanticResult.TypeSystem, interner, asmName.Name!);
        var functions = typeEmitter.EmitAssembly(mob);

        var mainMb = functions
                         .Select(f => f.Method)
                         .FirstOrDefault(m => m.Name == "main" && m.GetParameters().Length == 0)
                     ?? throw new InvalidOperationException("No parameterless main().");

        var md = ab.GenerateMetadata(out var il, out var fieldData);
        var peBuilder = new ManagedPEBuilder(
            header: PEHeaderBuilder.CreateExecutableHeader(),
            metadataRootBuilder: new MetadataRootBuilder(md),
            ilStream: il,
            mappedFieldData: fieldData,
            entryPoint: MetadataTokens.MethodDefinitionHandle(mainMb.MetadataToken));

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);

        using var ms = new MemoryStream();
        peBlob.WriteContentTo(ms);
        ms.Position = 0;
        var assembly = AssemblyLoadContext.Default.LoadFromStream(ms);

        var entry = assembly.GetType(asmName.Name!)?.GetMethod("main")
                    ?? throw new InvalidOperationException("Entry not found.");

        return new EmittedImage(asmName.Name!, assembly, entry, functions);
    }

    public BuildResult EmitToDisk(string outputDir)
    {
        Directory.CreateDirectory(outputDir);
        var mob = ab.DefineDynamicModule(asmName.Name!);
        var functions = new CilCompilationUnitEmitter(semanticResult.Ir, semanticResult.SymbolTable,
            semanticResult.TypeSystem, interner, asmName.Name!).EmitAssembly(mob);

        var mainMb = functions.Select(f => f.Method)
            .First(m => m.Name == "main" && m.GetParameters().Length == 0);

        var md = ab.GenerateMetadata(out var il, out var fieldData);
        var peBuilder = new ManagedPEBuilder(
            header: PEHeaderBuilder.CreateExecutableHeader(),
            metadataRootBuilder: new MetadataRootBuilder(md),
            ilStream: il,
            mappedFieldData: fieldData,
            entryPoint: MetadataTokens.MethodDefinitionHandle(mainMb.MetadataToken));

        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);

        var basePath = Path.Combine(outputDir, asmName.Name!);

        var dllPath = basePath + ".dll";

        using (var fs = File.Create(dllPath)) peBlob.WriteContentTo(fs);

        const string runtimeConfig = """
                                     {
                                         "runtimeOptions": {
                                           "tfm": "net9.0",
                                           "framework": {
                                             "name": "Microsoft.NETCore.App",
                                             "version": "9.0.0"
                                           }
                                         }
                                       }

                                     """;

        var runtimeConfigpath = basePath + ".runtimeconfig.json";
        File.WriteAllText(runtimeConfigpath, runtimeConfig);

        return new BuildResult(dllPath, runtimeConfigpath);
    }
}
