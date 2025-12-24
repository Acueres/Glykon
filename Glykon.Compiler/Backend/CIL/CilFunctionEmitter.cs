using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

internal class CilFunctionEmitter
{
    readonly MethodBuilder mb;
    readonly ILGenerator il;
    readonly TypeSystem typeSystem;

    readonly IRFunctionDeclaration functionDeclaration;

    Dictionary<FunctionSymbol, MethodInfo> combinedFunctions = [];
    readonly Dictionary<FunctionSymbol, MethodInfo> localFunctions = [];
    readonly List<CilFunctionEmitter> functionEmitters = [];

    readonly Label? returnLabel;
    readonly LocalBuilder? returnLocal;

    public CilFunctionEmitter(IRFunctionDeclaration functionDeclaration, TypeSystem typeSystem, IdentifierInterner interner,
        TypeBuilder typeBuilder)
    {
        this.functionDeclaration = functionDeclaration;
        this.typeSystem = typeSystem;

        var parameterTypes = IntrinsicClrTypeTranslator.Translate([.. functionDeclaration.Parameters.Select(p => p.Type)]);
        var returnType = IntrinsicClrTypeTranslator.Translate(functionDeclaration.ReturnType);

        string name = interner[functionDeclaration.Signature.QualifiedNameId];

        mb = typeBuilder.DefineMethod(name,
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
            returnType, parameterTypes);

        il = mb.GetILGenerator();

        for (int i = 0; i < functionDeclaration.Parameters.Length; i++)
        {
            string paramName = interner[functionDeclaration.Parameters[i].NameId];
            mb.DefineParameter(i + 1, ParameterAttributes.None, paramName);
        }

        int n = CountReturnStatements(functionDeclaration);
        bool multipleReturns = n > 1;

        if (multipleReturns || (functionDeclaration.ReturnType.Kind == TypeKind.None && n > 0))
        {
            returnLabel = il.DefineLabel();
        }

        if (functionDeclaration.ReturnType.Kind != TypeKind.None && multipleReturns)
        {
            returnLocal = il.DeclareLocal(IntrinsicClrTypeTranslator.Translate(functionDeclaration.ReturnType));
        }

        var locals = functionDeclaration.Body.Statements
            .Where(s => s.Kind == IRStatementKind.Function).Cast<IRFunctionDeclaration>();
        foreach (var f in locals)
        {
            CilFunctionEmitter mg = new(f, typeSystem, interner, typeBuilder);
            functionEmitters.Add(mg);
            localFunctions.Add(f.Signature, mg.GetMethodBuilder());
        }
    }

    public MethodBuilder GetMethodBuilder() => mb;

    public void Emit(Dictionary<FunctionSymbol, MethodInfo> functions)
    {
        combinedFunctions = functions.Concat(localFunctions).ToDictionary();

        CilEmitContext context = new()
        {
            Functions = combinedFunctions,
            ReturnLabel = returnLabel,
            ReturnLocal = returnLocal
        };
        CilCodeGenerator codeGenerator = new CilCodeGenerator(il, typeSystem, context);
        
        codeGenerator.EmitStatements(functionDeclaration.Body.Statements);

        foreach (var mg in functionEmitters)
        {
            mg.Emit(combinedFunctions);
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

    static int CountReturnStatements(IRStatement statement)
    {
        if (statement.Kind == IRStatementKind.Return)
        {
            return 1;
        }

        int count = 0;
        if (statement is IRFunctionDeclaration fStmt)
        {
            count += CountReturnStatements(fStmt.Body);
        }
        else if (statement is IRBlockStmt blockStmt)
        {
            foreach (var stmt in blockStmt.Statements)
            {
                count += CountReturnStatements(stmt);
            }
        }
        else if (statement is IRIfStmt ifStmt)
        {
            count += CountReturnStatements(ifStmt.ThenStatement);

            if (ifStmt.ElseStatement is not null)
            {
                count += CountReturnStatements(ifStmt.ElseStatement);
            }
        }
        else if (statement is IRWhileStmt whileStmt)
        {
            count += CountReturnStatements(whileStmt.Body);
        }

        return count;
    }
}