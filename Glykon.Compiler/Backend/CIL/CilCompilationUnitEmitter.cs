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
    IdentifierInterner interner,
    string appName)
{
    private readonly ClrTypeRegistry typeRegistry = new();

    public FunctionInfo[] EmitAssembly(ModuleBuilder mob)
    {
        symbolTable.ResetScope();

        TypeBuilder unitTb = mob.DefineType(appName,
            TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.Abstract | TypeAttributes.Sealed);

        List<IRFunctionDeclaration> functionDeclarations = [];
        List<IRTypeDeclaration> classDeclarations = [];

        foreach (var stmt in irTree)
        {
            switch (stmt)
            {
                case IRFunctionDeclaration f:
                    functionDeclarations.Add(f);
                    break;
                case IRTypeDeclaration c:
                    classDeclarations.Add(c);
                    break;
            }
        }
        
        List<CilCallableEmitter> callableEmitters = [];
        Dictionary<FunctionSymbol, MethodInfo> functions = LoadStdLibrary();
        List<FunctionInfo> definedFunctions = [];
        Dictionary<MethodSymbol, MethodInfo> methods = [];
        Dictionary<FieldSymbol, FieldInfo> fields = [];

        // Create top-level emitters
        var typeEmitters = classDeclarations.Select(c => new CilTypeEmitter(c, mob, interner, typeRegistry)).ToArray();
        
        // Define all types (top-level and nested)
        foreach (var typeEmitter in typeEmitters)
        {
            typeEmitter.DefineType();
        }
        
        Dictionary<TypeSymbol, ConstructorInfo> constructors = [];
        // Define constructors for all types
        foreach (var typeEmitter in typeEmitters)
        {
            foreach (var (type, constructor) in typeEmitter.DefineConstructors())
            {
                constructors[type] = constructor;
            }
        }
        
        // Emit all fields
        foreach (var typeEmitter in typeEmitters)
        {
            var definedFields = typeEmitter.EmitFields();
            foreach (var (fieldSymbol, fieldInfo) in definedFields)
            {
                fields[fieldSymbol] = fieldInfo;
            }
        }
        
        // Define all methods
        foreach (var typeEmitter in typeEmitters)
        {
            var methodEmitters = typeEmitter.DefineMethods();
            foreach (var (signature, methodEmitter) in methodEmitters)
            {
                callableEmitters.Add(methodEmitter);
                var mb = methodEmitter.GetMethodBuilder();
                methods[signature] = mb;
            }
        }

        // Define top-level functions
        foreach (var f in functionDeclarations)
        {
            CilCallableEmitter emitter = new(f, typeRegistry, interner, unitTb);
            callableEmitters.Add(emitter);
            
            var mb = emitter.GetMethodBuilder();
            functions[f.Signature] = mb;
            definedFunctions.Add(new FunctionInfo(f.Signature, mb));
        }
        
        // Define all locals
        List<CilCallableEmitter> localEmitters = [];
        foreach (var locals in callableEmitters
                     .Select(callable => callable.DefineLocals()))
        {
            foreach (var (local, signature) in locals)
            {
                localEmitters.Add(local);

                var mb = local.GetMethodBuilder();
                functions[signature] = mb;
                definedFunctions.Add(new FunctionInfo(signature, mb));
            }
        }
        
        // Add locals to the emitter pile
        callableEmitters.AddRange(localEmitters);
        
        // Emit callable bodies
        foreach (var callableEmitter in callableEmitters)
        {
            callableEmitter.Emit(functions, methods, fields, constructors, callableEmitter.ParentType,
                callableEmitter.IsStatic);
        }

        // Create defined types
        foreach (var typeEmitter in typeEmitters)
        {
            typeEmitter.CreateType();
        }
        
        unitTb.CreateType();

        return [..definedFunctions];
    }

    private Dictionary<FunctionSymbol, MethodInfo> LoadStdLibrary()
    {
        Dictionary<FunctionSymbol, MethodInfo> stdFunctions = [];

        var console = typeof(Console);

        stdFunctions.Add(symbolTable.GetFunction("println")!,
            console.GetMethod("WriteLine", [typeof(string)])!);

        return stdFunctions;
    }
}