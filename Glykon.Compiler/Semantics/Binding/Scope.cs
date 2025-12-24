using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding;

public enum ScopeKind
{
    Top,
    Function,
    Method,
    Type,
    Block
}

public class Scope
{
    public Scope? Parent { get; }
    public ScopeKind Kind { get; }

    public FunctionSymbol? ContainingFunction { get; }
    public MethodSymbol? ContainingMethod { get; }

    readonly Dictionary<int, Symbol> symbols = [];
    readonly Dictionary<int, FunctionSymbol> functions = [];
    readonly Dictionary<int, MethodSymbol> methods = [];
    readonly Dictionary<int, TypeSymbol> types = [];

    int parameterCount;

    public Scope(Scope parent, ScopeKind scopeKind)
    {
        Parent = parent;
        Kind = scopeKind;
        ContainingFunction = parent?.ContainingFunction;
    }

    public Scope(Scope parent, FunctionSymbol function)
    {
        Parent = parent;
        Kind = ScopeKind.Function;
        ContainingFunction = function;
    }
    
    public Scope(Scope parent, MethodSymbol method)
    {
        Parent = parent;
        Kind = ScopeKind.Method;
        ContainingMethod = method;
    }

    public Scope() { Kind = ScopeKind.Top; }

    public FunctionSymbol? GetLocalFunction(int id)
    {
        return functions.GetValueOrDefault(id);
    }

    public FunctionSymbol? AddFunction(int symbolId, int serialId, int qualifiedId, TypeSymbol returnType,
        TypeSymbol[] parameters)
    {
        FunctionSymbol symbol = new(symbolId, serialId, qualifiedId, returnType, parameters);

        functions.Add(symbolId, symbol);

        return symbol;
    }

    public FunctionSymbol? GetFunction(int id)
    {
        if (!functions.TryGetValue(id, out var symbol))
        {
            return Parent?.GetFunction(id);
        }
        
        return symbol;
    }

    public MethodSymbol? AddMethod(int symbolId, TypeSymbol returnType, TypeSymbol parentType, TypeSymbol[] parameters,
        bool isStatic)
    {
        MethodSymbol symbol = new(symbolId, returnType, parentType, parameters, isStatic);

        methods.Add(symbolId, symbol);

        return symbol;
    }

    public MethodSymbol? GetMethod(int id)
    {
        if (!methods.TryGetValue(id, out var symbol))
        {
            return Parent?.GetMethod(id);
        }
        
        return symbol;
    }

    public ConstantSymbol RegisterConstant(int id, TypeSymbol type)
    {
        ConstantSymbol symbol = new(id, type);
        symbols.Add(id, symbol);
        return symbol;
    }

    public ParameterSymbol AddParameter(int id, TypeSymbol type)
    {
        ParameterSymbol symbol = new(id, type, parameterCount++);
        symbols.Add(id, symbol);
        return symbol;
    }

    public VariableSymbol AddVariable(int id, bool immutable, TypeSymbol type)
    {
        VariableSymbol symbol = new(id, immutable, type);
        symbols.Add(id, symbol);
        return symbol;
    }

    public VariableSymbol? GetVariable(int id)
    {
        if (!symbols.TryGetValue(id, out Symbol? symbol) || symbol is not VariableSymbol variable)
        {
            if (Kind is ScopeKind.Function or ScopeKind.Method) return null;
            return Parent?.GetVariable(id);
        }
        
        return variable;
    }
    
    public FieldSymbol AddField(int id, TypeSymbol type, TypeSymbol parentType)
    {
        FieldSymbol symbol = new(id, type, parentType);
        symbols.Add(id, symbol);
        return symbol;
    }

    public FieldSymbol? GetField(int id)
    {
        if (!symbols.TryGetValue(id, out var symbol) || symbol is not FieldSymbol field)
        {
            return Parent?.GetField(id);
        }
        
        return field;
    }

    public void AddType(int id, TypeSymbol type)
    {
        types.Add(id, type);
    }

    public TypeSymbol? GetType(int id)
    {
        if (!types.TryGetValue(id, out var symbol))
        {
            return Parent?.GetType(id);
        }

        return symbol;
    }

    public Symbol? GetSymbol(int id)
    {
        if (!symbols.TryGetValue(id, out var symbol))
        {
            return Parent?.GetSymbol(id);
        }

        return symbol;
    }
}
