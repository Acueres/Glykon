using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics;

public class SymbolTable
{
    readonly Scope global = new();
    readonly List<Scope> scopes;
    readonly IdentifierInterner interner;

    Scope current;

    public SymbolTable(IdentifierInterner interner)
    {
        scopes = [global];
        current = global;
        this.interner = interner;
    }

    public FunctionSymbol? GetCurrentContainingFunction()
    {
        return current.ContainingFunction;
    }

    public FunctionSymbol? RegisterFunction(string name, TokenType returnType, TokenType[] parameterTypes)
    {
        int symbolIndex = interner.Intern(name);
        FunctionSymbol? signature = current.AddFunction(symbolIndex, returnType, parameterTypes);
        return signature;
    }

    public FunctionSymbol? GetFunction(string name, TokenType[] parameters)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetFunction(id, parameters);
    }

    public bool IsFunction(string name)
    {
        if (!interner.TryGetId(name, out var id)) return false;
        return current.GetFunctionOverloads(id).Count > 0;
    }

    public ConstantSymbol RegisterConstant(string name, object value, TokenType type)
    {
        int symbolIndex = interner.Intern(name);
        ConstantSymbol constant = current.AddConstant(symbolIndex, value, type);
        return constant;
    }

    public ParameterSymbol RegisterParameter(string name, TokenType type)
    {
        int symbolIndex = interner.Intern(name);
        ParameterSymbol parameter = current.AddParameter(symbolIndex, type);
        return parameter;
    }

    public VariableSymbol RegisterVariable(string name, TokenType type)
    {
        int symbolIndex = interner.Intern(name);
        VariableSymbol variable = current.AddVariable(symbolIndex, type);
        return variable;
    }

    public Symbol? GetSymbol(string name)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetSymbol(id);
    }

    public VariableSymbol? GetVariable(string name)
    {
        if (!interner.TryGetId(name, out var id)) return null;
        return current.GetVariable(id);
    }

    public int BeginScope(ScopeKind scopeKind)
    {
        int index = scopes.Count;
        current = new Scope(current, index, scopeKind);
        scopes.Add(current);
        return index;
    }

    public int BeginScope(FunctionSymbol containingFunction)
    {
        int index = scopes.Count;
        current = new Scope(current, index, containingFunction);
        scopes.Add(current);
        return index;
    }

    public void ExitScope()
    {
        current = current.Parent;
    }

    public void EnterScope(int index)
    {
        current = scopes[index];
    }

    public void ResetScope()
    {
        current = global;
    }

    public bool UpdateType(string name, TokenType type)
    {
        if (!interner.TryGetId(name, out var id)) return false;
        return current.UpdateSymbolType(id, type);
    }
}
