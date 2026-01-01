using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Backend.CIL;

public sealed class CilCallableEmitter
{
    private readonly TypeBuilder tb;
    private readonly MethodBuilder mb;
    private readonly ILGenerator il;
    
    private readonly IdentifierInterner interner;
    private readonly ClrTypeRegistry typeRegistry;
    
    private readonly IRBlockStmt body;
    private readonly ParameterSymbol[] parameters;
    private readonly List<IRFunctionDeclaration> locals = [];
    private readonly TypeSymbol returnType;

    public CilCallableEmitter(IRMethodDeclaration methodDeclaration,
        ClrTypeRegistry typeRegistry,
        IdentifierInterner interner,
        TypeBuilder tb) : this(tb, typeRegistry, interner, methodDeclaration.Body, methodDeclaration.Parameters,
        methodDeclaration.ReturnType)
    {
        var parameterTypes = GetParameterTypes();
        var clrReturnType = GetReturnType();

        var attrs = MethodAttributes.HideBySig | MethodAttributes.Public;
        if (methodDeclaration.IsStatic)
        {
            attrs |= MethodAttributes.Static;
        }

        string name = interner[methodDeclaration.Signature.NameId];
        mb = tb.DefineMethod(name, attrs, clrReturnType, parameterTypes);

        il = mb.GetILGenerator();
    }

    public CilCallableEmitter(IRFunctionDeclaration functionDeclaration,
        ClrTypeRegistry typeRegistry,
        IdentifierInterner interner,
        TypeBuilder tb) : this(tb, typeRegistry, interner, functionDeclaration.Body, functionDeclaration.Parameters,
        functionDeclaration.ReturnType)
    {
        var parameterTypes = GetParameterTypes();
        var clrReturnType = GetReturnType();

        string name = interner[functionDeclaration.Signature.NameId];
        mb = tb.DefineMethod(name,
            MethodAttributes.HideBySig | MethodAttributes.Public | MethodAttributes.Static,
            clrReturnType, parameterTypes);

        il = mb.GetILGenerator();
    }

    private CilCallableEmitter(TypeBuilder tb, ClrTypeRegistry typeRegistry, IdentifierInterner interner,
        IRBlockStmt body, ParameterSymbol[] parameters, TypeSymbol returnType)
    {
        this.tb = tb;
        this.interner = interner;
        this.typeRegistry = typeRegistry;
        
        this.body = body;
        this.parameters = parameters;
        this.returnType = returnType;
        
        CollectLocals(body);
    }
    
    public MethodBuilder GetMethodBuilder() => mb;

    public (CilCallableEmitter, FunctionSymbol)[] DefineLocals()
    {
        List<(CilCallableEmitter, FunctionSymbol)> localEmitters = [];
        foreach (var localDecl in locals)
        {
            var localEmitter = new CilCallableEmitter(localDecl, typeRegistry, interner, tb);
            localEmitters.Add((localEmitter, localDecl.Signature));
            localEmitters.AddRange(localEmitter.DefineLocals());
        }
        
        return localEmitters.ToArray();
    }

    public void Emit(Dictionary<FunctionSymbol, MethodInfo> functions,
        Dictionary<MethodSymbol, MethodInfo> methods, Dictionary<FieldSymbol, FieldInfo> fields)
    {
        DefineParameterMetadata();
        
        // Plan return path
        int n = CountReturnStatements(body);
        bool multipleReturns = n > 1;

        Label? returnLabel = null;
        if (multipleReturns || (returnType.Kind == TypeKind.None && n > 0))
        {
            returnLabel = il.DefineLabel();
        }

        LocalBuilder? returnLocal = null;
        if (returnType.Kind != TypeKind.None && multipleReturns)
        {
            returnLocal = il.DeclareLocal(typeRegistry.Resolve(returnType));
        }
        
        CilEmitContext context = new()
        {
            Functions = functions,
            Methods = methods,
            ReturnLabel = returnLabel,
            ReturnLocal = returnLocal
        };
        var codeGenerator = new CilCodeGenerator(il, context, typeRegistry);
        
        codeGenerator.EmitStatements(body.Statements);
        
        // Emit return
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
    
    private void DefineParameterMetadata()
    {
        for (int i = 0; i < parameters.Length; i++)
        {
            string paramName = interner[parameters[i].NameId];
            mb.DefineParameter(i + 1, ParameterAttributes.None, paramName);
        }
    }
    
    private Type[] GetParameterTypes()
    {
        var parameterTypes = parameters.Select(p => typeRegistry.Resolve(p.Type)).ToArray();
        return parameterTypes;
    }

    private Type GetReturnType()
    {
        return typeRegistry.Resolve(returnType);
    }

    private void CollectLocals(IRStatement stmt)
    {
        while (true)
        {
            switch (stmt)
            {
                case IRFunctionDeclaration f:
                    locals.Add(f);
                    break;
                case IRBlockStmt b:
                {
                    foreach (var s in b.Statements)
                    {
                        CollectLocals(s);
                    }

                    break;
                }
                case IRIfStmt iff:
                {
                    CollectLocals(iff.ThenStatement);
                    if (iff.ElseStatement is not null)
                    {
                        stmt = iff.ElseStatement;
                        continue;
                    }

                    break;
                }
                case IRWhileStmt w:
                {
                    stmt = w.Body;
                    continue;
                }
            }

            break;
        }
    }

    private static int CountReturnStatements(IRStatement statement)
    {
        if (statement.Kind == IRStatementKind.Return)
        {
            return 1;
        }

        if (statement.Kind is IRStatementKind.Function or IRStatementKind.Method)
        {
            return 0;
        }

        int count = 0;
        switch (statement)
        {
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