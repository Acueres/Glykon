using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public class CilCompilationUnitEmitter(
    IRTree irTree,
    SymbolTable symbolTable,
    TypeSystem typeSystem,
    IdentifierInterner interner,
    string appName)
{
    public FunctionInfo[] EmitAssembly(ModuleBuilder mob)
    {
        symbolTable.ResetScope();

        TypeBuilder tb = mob.DefineType(appName,
            TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);

        List<CilFunctionEmitter> functionEmitters = [];
        Dictionary<FunctionSymbol, MethodInfo> stdFunctions = LoadStdLibrary();
        List<FunctionInfo> definedFunctions = [];

        foreach (var stmt in irTree)
        {
            if (stmt is IRFunctionDeclaration f)
            {
                CilFunctionEmitter mg = new(f, typeSystem, interner, tb);
                functionEmitters.Add(mg);

                var mb = mg.GetMethodBuilder();
                stdFunctions[f.Signature] = mb;
                definedFunctions.Add(new FunctionInfo(f.Signature, mb));
            }
            else if (stmt is IRClassDeclaration c)
            {
                
            }
        }

        foreach (var mg in functionEmitters)
        {
            mg.Emit(stdFunctions);
        }

        tb.CreateType();

        return [..definedFunctions];
    }

    Dictionary<FunctionSymbol, MethodInfo> LoadStdLibrary()
    {
        Dictionary<FunctionSymbol, MethodInfo> stdFunctions = [];

        var console = typeof(Console);

        stdFunctions.Add(symbolTable.GetFunction("println")!,
            console.GetMethod("WriteLine", [typeof(string)])!);

        return stdFunctions;
    }
}