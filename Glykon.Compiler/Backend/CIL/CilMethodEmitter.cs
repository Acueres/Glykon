using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

internal class CilMethodEmitter
{
    private readonly MethodBuilder mb;
    private readonly ILGenerator il;
    private readonly TypeBuilder typeBuilder;
    
    private readonly TypeSystem typeSystem;
    private readonly IdentifierInterner interner;

    private readonly IRMethodDeclaration methodDeclaration;

    public CilMethodEmitter(IRMethodDeclaration methodDeclaration, TypeSystem typeSystem, IdentifierInterner interner,
        TypeBuilder typeBuilder)
    {
        this.methodDeclaration = methodDeclaration;
        this.typeSystem = typeSystem;
        this.interner = interner;
        this.typeBuilder = typeBuilder;

        var parameterTypes = IntrinsicClrTypeTranslator.Translate([..methodDeclaration.Parameters.Select(p => p.Type)]);
        var returnType = IntrinsicClrTypeTranslator.Translate(methodDeclaration.ReturnType);

        string name = interner[methodDeclaration.Symbol.NameId];

        mb = typeBuilder.DefineMethod(name,
            MethodAttributes.HideBySig | MethodAttributes.Private,
            returnType, parameterTypes);

        il = mb.GetILGenerator();
    }

    public MethodBuilder GetMethodBuilder() => mb;

    public void Emit(Dictionary<FunctionSymbol, MethodInfo> functions, Dictionary<MethodSymbol, MethodInfo> methods)
    {
        for (int i = 0; i < methodDeclaration.Parameters.Length; i++)
        {
            string paramName = interner[methodDeclaration.Parameters[i].NameId];
            mb.DefineParameter(i + 1, ParameterAttributes.None, paramName);
        }
        
        Dictionary<FunctionSymbol, MethodInfo> localFunctions = [];
        List<CilFunctionEmitter> functionEmitters = [];
        
        var locals = methodDeclaration.Body.Statements
            .Where(s => s.Kind == IRStatementKind.Function).Cast<IRFunctionDeclaration>();
        foreach (var f in locals)
        {
            CilFunctionEmitter mg = new(f, typeSystem, interner, typeBuilder);
            functionEmitters.Add(mg);
            localFunctions.Add(f.Signature, mg.GetMethodBuilder());
        }
        
        var combinedFunctions = functions.Concat(localFunctions).ToDictionary();
        
        int n = CountReturnStatements(methodDeclaration);
        bool multipleReturns = n > 1;

        Label? returnLabel = null;
        if (multipleReturns || (methodDeclaration.ReturnType.Kind == TypeKind.None && n > 0))
        {
            returnLabel = il.DefineLabel();
        }
        
        LocalBuilder? returnLocal = null;
        if (methodDeclaration.ReturnType.Kind != TypeKind.None && multipleReturns)
        {
            returnLocal = il.DeclareLocal(IntrinsicClrTypeTranslator.Translate(methodDeclaration.ReturnType));
        }
        
        CilEmitContext context = new()
        {
            Functions = combinedFunctions,
            Methods = methods,
            ReturnLabel = returnLabel,
            ReturnLocal = returnLocal
        };
        CilCodeGenerator codeGenerator = new CilCodeGenerator(il, typeSystem, context);
        
        codeGenerator.EmitStatements(methodDeclaration.Body.Statements);

        foreach (var f in functionEmitters)
        {
            f.Emit(combinedFunctions);
        }

        if (returnLabel is not null)
        {
            il.MarkLabel((Label)returnLabel);
        }

        if (returnLocal is not null)
        {
            il.Emit(OpCodes.Ldloc, returnLocal.LocalIndex);
        }

        il.Emit(OpCodes.Ret);
    }

    private static int CountReturnStatements(IRStatement statement)
    {
        if (statement.Kind == IRStatementKind.Return)
        {
            return 1;
        }

        int count = 0;
        switch (statement)
        {
            case IRFunctionDeclaration fStmt:
                count += CountReturnStatements(fStmt.Body);
                break;
            case IRBlockStmt blockStmt:
                count += blockStmt.Statements.Sum(CountReturnStatements);
                break;
            case IRIfStmt ifStmt:
            {
                count += CountReturnStatements(ifStmt.ThenStatement);

                if (ifStmt.ElseStatement is not null)
                {
                    count += CountReturnStatements(ifStmt.ElseStatement);
                }

                break;
            }
            case IRWhileStmt whileStmt:
                count += CountReturnStatements(whileStmt.Body);
                break;
        }

        return count;
    }
}