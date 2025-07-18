using TythonCompiler.SemanticAnalysis.Symbols;
using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis;

public class SymbolTable
{
    readonly Scope global = new();
    readonly List<Scope> scopes;
    readonly Dictionary<string, int> symbolMap = [];

    Scope current;

    public SymbolTable()
    {
        scopes = [global];
        current = global;
    }

    public FunctionSymbol? GetCurrentContainingFunction()
    {
        return current.ContainingFunction;
    }

    public FunctionSymbol? RegisterFunction(string name, TokenType returnType, TokenType[] parameterTypes)
    {
        int symbolIndex = TryAddSymbolId(name);
        FunctionSymbol? signature = current.AddFunction(symbolIndex, returnType, parameterTypes);
        return signature;
    }

    public FunctionSymbol? GetFunction(string name, TokenType[] parameters)
    {
        int symbolIndex = symbolMap[name];
        FunctionSymbol? function = current.GetFunction(symbolIndex, parameters);
        return function;
    }

    public bool IsFunction(string name)
    {
        int symbolIndex = symbolMap[name];
        var overloads = current.GetFunctionOverloads(symbolIndex);
        return overloads.Count > 0;
    }

    public ConstantSymbol RegisterConstant(string name, object value, TokenType type)
    {
        int symbolIndex = TryAddSymbolId(name);
        ConstantSymbol constant = current.AddConstant(symbolIndex, value, type);
        return constant;
    }

    public ParameterSymbol RegisterParameter(string name, TokenType type)
    {
        int symbolIndex = TryAddSymbolId(name);
        ParameterSymbol parameter = current.AddParameter(symbolIndex, type);
        return parameter;
    }

    public VariableSymbol RegisterVariable(string name, TokenType type)
    {
        int symbolIndex = TryAddSymbolId(name);
        VariableSymbol variable = current.AddVariable(symbolIndex, type);
        return variable;
    }

    public Symbol? GetSymbol(string name)
    {
        int symbolIndex = symbolMap[name];
        Symbol? symbol = current.GetSymbol(symbolIndex);
        return symbol;
    }

    public VariableSymbol? GetVariable(string name)
    {
        int symbolIndex = symbolMap[name];
        VariableSymbol? variable = current.GetVariable(symbolIndex);
        return variable;
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
        int symbolIndex = symbolMap[name];

        return current.UpdateSymbolType(symbolIndex, type);
    }

    public int TryAddSymbolId(string name)
    {
        if (!symbolMap.TryGetValue(name, out int symbolIndex))
        {
            symbolIndex = symbolMap.Count;
            symbolMap.Add(name, symbolIndex);
        }

        return symbolIndex;
    }
}
