using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Binding;

public enum ScopeKind
{
    Top,
    Function,
    Block
}

public class Scope
{
    public Scope Parent { get; }
    public ScopeKind Kind { get; }

    public FunctionSymbol? ContainingFunction { get; }

    readonly Dictionary<int, Symbol> symbols = [];
    readonly Dictionary<int, List<FunctionSymbol>> functions = [];
    readonly Dictionary<int, TypeSymbol> types = [];

    int parameterCount = 0;

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

    public Scope() { Kind = ScopeKind.Top; }

    public FunctionSymbol? GetLocalFunction(int id, TypeSymbol[] parameters)
    {
        if (functions.TryGetValue(id, out var localOverloads))
        {
            foreach (var overload in localOverloads)
            {
                if (overload.Parameters.SequenceEqual(parameters))
                {
                    return overload;
                }
            }
        }

        return null;
    }

    public FunctionSymbol? AddFunction(int symbolId, int serialId, int qualifiedId, TypeSymbol returnType, TypeSymbol[] parameters)
    {
        FunctionSymbol symbol;

        if (functions.TryGetValue(symbolId, out List<FunctionSymbol>? overloads))
        {
            foreach (var overload in overloads)
            {
                if (parameters.Length == overload.Parameters.Length)
                {
                    if (parameters.SequenceEqual(overload.Parameters))
                    {
                        return null;
                    }
                }
            }

            symbol = new(symbolId, serialId, qualifiedId, returnType, parameters);
            overloads.Add(symbol);
        }
        else
        {
            symbol = new(symbolId, serialId, qualifiedId, returnType, parameters);
            functions.Add(symbolId, [symbol]);
        }

        return symbol;
    }

    public FunctionSymbol? GetFunction(int id, TypeSymbol[] parameters)
    {
        var allOverloads = GetFunctionOverloads(id);

        foreach (var overload in allOverloads)
        {
            if (overload.Parameters.SequenceEqual(parameters))
            {
                return overload;
            }
        }

        return null;
    }

    public FunctionSymbol[] GetFunctionOverloads(int id)
    {
        List<FunctionSymbol> allOverloads = [];

        if (functions.TryGetValue(id, out List<FunctionSymbol>? localOverloads))
        {
            allOverloads.AddRange(localOverloads);
        }

        if (Parent is not null)
        {
            allOverloads.AddRange(Parent.GetFunctionOverloads(id));
        }

        return [..allOverloads];
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

    public VariableSymbol AddVariable(int id, TypeSymbol type)
    {
        VariableSymbol symbol = new(id, type);
        symbols.Add(id, symbol);
        return symbol;
    }

    public VariableSymbol? GetVariable(int id)
    {
        if (!symbols.TryGetValue(id, out Symbol? symbol) || symbol is not VariableSymbol variable)
        {
            if (Kind == ScopeKind.Function) return null;
            return Parent is null ? null : Parent.GetVariable(id);
        }
        
        return variable;
    }

    public void AddType(int id, TypeSymbol type)
    {
        types.Add(id, type);
    }

    public TypeSymbol? GetType(int id)
    {
        if (!types.TryGetValue(id, out TypeSymbol? symbol))
        {
            if (Parent is null)
            {
                return null;
            }

            return Parent.GetType(id);
        }

        return symbol;
    }

    public Symbol? GetSymbol(int id)
    {
        if (!symbols.TryGetValue(id, out Symbol? symbol))
        {
            if (Parent is null)
            {
                return null;
            }

            return Parent.GetSymbol(id);
        }

        return symbol;
    }
}
