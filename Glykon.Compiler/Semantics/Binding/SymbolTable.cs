using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding;

public class SymbolTable
{
    readonly Scope top = new();
    readonly List<Scope> scopes;
    readonly IdentifierInterner interner;

    Scope current;

    int functionSerial;

    public SymbolTable(IdentifierInterner interner)
    {
        scopes = [top];
        current = top;
        this.interner = interner;
    }

    public FunctionSymbol? GetCurrentFunction()
    {
        return current.ContainingFunction;
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

    public FunctionSymbol? GetFunction(string name, TypeSymbol[] parameters)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetFunction(id, parameters);
    }

    public FunctionSymbol[] GetFunctionOverloads(string name)
    {
        if (!interner.TryGetId(name, out var id)) return [];
        return current.GetFunctionOverloads(id);
    }

    public FunctionSymbol? GetLocalFunction(string name, TypeSymbol[] parameters)
    {
        if (!interner.TryGetId(name, out var nameId)) return null;
        return current.GetLocalFunction(nameId, parameters);
    }

    public TypeSymbol? GetType(string name)
    {
        if (!interner.TryGetId(name, out var nameId)) return null;
        return current.GetType(nameId);
    }
    
    public FunctionSymbol? RegisterFunction(string name, TypeSymbol returnType, TypeSymbol[] parameterTypes)
    {
        int symbolIndex = interner.Intern(name);
        string qualifiedName = ComputeQualifiedName(name);
        int qualifiedId = interner.Intern(qualifiedName);
        FunctionSymbol? signature = current.AddFunction(symbolIndex, functionSerial++, qualifiedId, returnType, parameterTypes);
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

    public VariableSymbol RegisterVariable(string name, TypeSymbol type)
    {
        int symbolIndex = interner.Intern(name);
        VariableSymbol variable = current.AddVariable(symbolIndex, type);
        return variable;
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

    public void ExitScope()
    {
        current = current.Parent;
    }

    public void ResetScope()
    {
        current = top;
    }

    string ComputeQualifiedName(string localName)
    {
        var stack = GetContainingFunctionStack();
        return stack.Count == 0 ? localName : string.Join('.', stack.Append(localName));
    }

    List<string> GetContainingFunctionStack()
    {
        Scope currentScope = current;
        List<string> stack = [];

        while (currentScope != null)
        {
            if (currentScope.Kind == ScopeKind.Function && currentScope.ContainingFunction != null)
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
