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
    public List<FunctionInfo> EmitAssembly(ModuleBuilder mob)
    {
        symbolTable.ResetScope();

        TypeBuilder tb = mob.DefineType(appName,
            TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);

        List<CilFunctionEmitter> methodGenerators = [];
        Dictionary<FunctionSymbol, MethodInfo> methods = LoadStdLibrary();
        List<FunctionInfo> definedMethods = [];

        foreach (var stmt in irTree)
        {
            if (stmt is IRFunctionDeclaration f)
            {
                CilFunctionEmitter mg = new(f, typeSystem, interner, tb);
                methodGenerators.Add(mg);

                var mb = mg.GetMethodBuilder();
                methods[f.Signature] = mb;
                definedMethods.Add(new FunctionInfo(f.Signature, mb));
            }
        }

        foreach (var mg in methodGenerators)
        {
            mg.Emit(methods);
        }

        tb.CreateType();

        return definedMethods;
    }

    Dictionary<FunctionSymbol, MethodInfo> LoadStdLibrary()
    {
        Dictionary<FunctionSymbol, MethodInfo> stdFunctions = [];

        var console = typeof(Console);

        stdFunctions.Add(symbolTable.GetFunction("println", [typeSystem[TypeKind.String]]),
            console.GetMethod("WriteLine", [typeof(string)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [typeSystem[TypeKind.Int64]]),
            console.GetMethod("WriteLine", [typeof(long)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [typeSystem[TypeKind.Float64]]),
            console.GetMethod("WriteLine", [typeof(double)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [typeSystem[TypeKind.Bool]]),
            console.GetMethod("WriteLine", [typeof(bool)]));

        stdFunctions.Add(symbolTable.GetFunction("println", [typeSystem[TypeKind.None]]),
            console.GetMethod("WriteLine", []));

        return stdFunctions;
    }
}