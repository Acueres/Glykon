using CompilerService.SemanticAnalysis.Symbols;
using CompilerService.Tokenization;

namespace CompilerService.SemanticAnalysis;

public enum ScopeKind
{
    Top,
    Function,
    Block,
    Loop
}

public class Scope
{
    public Scope Parent { get; }
    public int Index { get; }
    public ScopeKind Kind { get; }

    public FunctionSymbol? ContainingFunction { get; }

    readonly Dictionary<int, Symbol> symbols = [];
    readonly Dictionary<int, List<FunctionSymbol>> functions = [];

    int parameterCount = 0;

    public Scope(Scope parent, int scopeIndex, ScopeKind scopeKind)
    {
        Parent = parent;
        Index = scopeIndex;
        Kind = scopeKind;
        ContainingFunction = parent?.ContainingFunction;
    }

    public Scope(Scope parent, int scopeIndex, FunctionSymbol function)
    {
        Parent = parent;
        Index = scopeIndex;
        Kind = ScopeKind.Function;
        ContainingFunction = function;
    }

    public Scope() { Kind = ScopeKind.Top; }

    public FunctionSymbol? AddFunction(int symbolId, TokenType returnType, TokenType[] parameters)
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

            symbol = new(symbolId, returnType, parameters);
            overloads.Add(symbol);
        }
        else
        {
            symbol = new(symbolId, returnType, parameters);
            functions.Add(symbolId, [symbol]);
        }

        return symbol;
    }

    public FunctionSymbol? GetFunction(int id, TokenType[] parameters)
    {
        List<FunctionSymbol> allOverloads = GetFunctionOverloads(id);

        foreach (var overload in allOverloads)
        {
            if (overload.Parameters.SequenceEqual(parameters))
            {
                return overload;
            }
        }

        return null;
    }

    public List<FunctionSymbol> GetFunctionOverloads(int id)
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

        return allOverloads;
    }

    public ConstantSymbol AddConstant(int id, object value, TokenType type)
    {
        ConstantSymbol symbol = new(id, type, value);
        symbols.Add(id, symbol);
        return symbol;
    }

    public ParameterSymbol AddParameter(int id, TokenType type)
    {
        ParameterSymbol symbol = new(id, type, parameterCount++);
        symbols.Add(id, symbol);
        return symbol;
    }

    public VariableSymbol AddVariable(int id, TokenType type)
    {
        VariableSymbol symbol = new(id, type);
        symbols.Add(id, symbol);
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

    public VariableSymbol? GetVariable(int id)
    {
        if (!symbols.TryGetValue(id, out Symbol? symbol))
        {
            if (Parent is null)
            {
                return null;
            }

            return Parent.GetVariable(id);
        }

        if (symbol is VariableSymbol variableSymbol)
            return variableSymbol;
        else return null;
    }

    public bool UpdateSymbolType(int id, TokenType type)
    {
        Symbol? symbol = GetSymbol(id);
        if (symbol is not null)
        {
            symbol.Type = type;
            return true;
        }

        return false;
    }
}
