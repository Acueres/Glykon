using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding;

public class SymbolTable
{
    private readonly Scope top = new();
    private readonly List<Scope> scopes;
    private readonly IdentifierInterner interner;

    private Scope current;

    private int functionSerial;

    public SymbolTable(IdentifierInterner interner)
    {
        scopes = [top];
        current = top;
        this.interner = interner;
    }

    public bool TryGetCurrentContainer(out Symbol? containerSymbol)
    {
        containerSymbol = null;

        if (current.ContainingFunction is not null)
        {
            containerSymbol = current.ContainingFunction;
            return true;
        }

        if (current.ContainingMethod is not null)
        {
            containerSymbol = current.ContainingMethod;
            return true;
        }

        return false;
    }
    
    public Symbol? GetSymbol(string name)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetSymbol(id);
    }

    public VariableSymbol? GetLocalVariableSymbol(string name)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetVariable(id);
    }

    public FunctionSymbol? GetFunction(string name)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetFunction(id);
    }

    public FunctionSymbol? GetLocalFunction(string name)
    {
        if (!interner.TryGetId(name, out var nameId)) return null;
        return current.GetLocalFunction(nameId);
    }
    
    public bool TryGetFunction(string name, out FunctionSymbol? function)
    {
        function = null;
        if (!interner.TryGetId(name, out var id)) return false;
        function = current.GetFunction(id);
        
        return function is not null;
    }

    public bool TryGetMethod(string name, out MethodSymbol? method)
    {
        method = null;
        if (!interner.TryGetId(name, out var id)) return false;
        method = current.GetMethod(id);
        
        return method is not null;
    }

    public bool TryGetType(string name, out TypeSymbol? type)
    {
        type = null;
        if (!interner.TryGetId(name, out var nameId)) return false;
        type = current.GetType(nameId);
        return type is not null;
    }
    
    public FunctionSymbol? RegisterFunction(string name, TypeSymbol returnType, TypeSymbol[] parameterTypes)
    {
        int symbolIndex = interner.Intern(name);
        string qualifiedName = ComputeQualifiedName(name);
        int qualifiedId = interner.Intern(qualifiedName);
        FunctionSymbol? signature = current.AddFunction(symbolIndex, functionSerial++, qualifiedId, returnType, parameterTypes);
        return signature;
    }

    public MethodSymbol? RegisterMethod(string name, TypeSymbol returnType, TypeSymbol parentType,
        TypeSymbol[] parameterTypes, bool isStatic)
    {
        int symbolIndex = interner.Intern(name);
        MethodSymbol? signature = current.AddMethod(symbolIndex, returnType, parentType, parameterTypes, isStatic);
        return signature;
    }

    public ConstantSymbol RegisterConstant(string name, TypeSymbol type)
    {
        int symbolIndex = interner.Intern(name);
        ConstantSymbol constant = current.RegisterConstant(symbolIndex, type);
        return constant;
    }

    public ParameterSymbol RegisterParameter(string name, TypeSymbol type)
    {
        int symbolIndex = interner.Intern(name);
        ParameterSymbol parameter = current.AddParameter(symbolIndex, type);
        return parameter;
    }

    public VariableSymbol RegisterVariable(string name, bool immutable, TypeSymbol type)
    {
        int symbolIndex = interner.Intern(name);
        VariableSymbol variable = current.AddVariable(symbolIndex, immutable, type);
        return variable;
    }
    
    public FieldSymbol RegisterField(string name, TypeSymbol parentType, TypeSymbol type)
    {
        int symbolIndex = interner.Intern(name);
        FieldSymbol field = current.AddField(symbolIndex, parentType, type);
        return field;
    }

    public void RegisterType(TypeSymbol type)
    {
        current.AddType(type.NameId, type);
    }

    public Scope GetCurrentScope() => current;

    public Scope BeginScope(ScopeKind scopeKind)
    {
        current = new Scope(current, scopeKind);
        scopes.Add(current);
        return current;
    }

    public Scope BeginScope(FunctionSymbol containingFunction)
    {
        current = new Scope(current, containingFunction);
        scopes.Add(current);
        return current;
    }
    
    public Scope BeginScope(MethodSymbol containingMethod)
    {
        current = new Scope(current, containingMethod);
        scopes.Add(current);
        return current;
    }

    public void EndScope()
    {
        current = current.Parent;
    }

    public void ResetScope()
    {
        current = top;
    }

    private string ComputeQualifiedName(string localName)
    {
        var stack = GetContainingFunctionStack();
        return stack.Count == 0 ? localName : string.Join('.', stack.Append(localName));
    }

    private List<string> GetContainingFunctionStack()
    {
        Scope currentScope = current;
        List<string> stack = [];

        while (currentScope != null)
        {
            if (currentScope is { Kind: ScopeKind.Function, ContainingFunction: not null })
            {
                string name = interner[currentScope.ContainingFunction.NameId];
                stack.Add(name);
            }

            currentScope = currentScope.Parent;
        }

        stack.Reverse();

        return stack;
    }
}
