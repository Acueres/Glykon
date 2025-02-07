using Tython.Model;

namespace Tython.Component
{
    public class SymbolTable
    {
        readonly Scope global = new();
        readonly List<Scope> scopes;
        readonly Dictionary<string, int> symbolMap = [];
        readonly Stack<int> localCounters = [];
        int localIndex;

        Scope current;

        public SymbolTable()
        {
            scopes = [global];
            current = global;
        }

        public FunctionSymbol RegisterFunction(string name, TokenType returnType, TokenType[] parameterTypes)
        {
            int symbolIndex = GetSymbolId(name);
            FunctionSymbol symbol = current.AddFunction(symbolIndex, returnType, parameterTypes);
            return symbol;
        }

        public FunctionSymbol? GetFunction(string name)
        {
            int symbolIndex = symbolMap[name];
            FunctionSymbol? function = current.GetFunction(symbolIndex);
            return function;
        }

        public ConstantSymbol RegisterConstant(string name, object value, TokenType type)
        {
            int symbolIndex = GetSymbolId(name);
            ConstantSymbol constant = current.AddConstant(symbolIndex, value, type);
            return constant;
        }

        public ConstantSymbol? GetConstant(string name)
        {
            int symbolIndex = symbolMap[name];
            ConstantSymbol? constant = current.GetConstant(symbolIndex);
            return constant;
        }

        public VariableSymbol RegisterVariable(string name, TokenType type)
        {
            int symbolIndex = GetSymbolId(name);
            VariableSymbol variable = current.AddVariable(localIndex++, symbolIndex, type);
            return variable;
        }

        public VariableSymbol? GetVariable(string name)
        {
            int symbolIndex = symbolMap[name];
            VariableSymbol? variable = current.GetVariable(symbolIndex);
            return variable;
        }

        public VariableSymbol? GetInitializedVariable(string name)
        {
            int symbolIndex = symbolMap[name];
            VariableSymbol? variable = current.GetInitializedVariable(symbolIndex);
            return variable;
        }

        public void Initializevariable(string name)
        {
            int symbolIndex = symbolMap[name];
            current.InitializeVariable(symbolIndex);
        }

        public int BeginScope()
        {
            int index = scopes.Count;
            current = new Scope(current, index);
            scopes.Add(current);
            return index;
        }

        public void ExitScope()
        {
            current = current.Root;
        }

        public void EnterScope(int index)
        {
            current = scopes[index];
        }

        public void ResetScope()
        {
            current = global;
        }

        public void BeginLocalCount()
        {
            localCounters.Push(localIndex);
            localIndex = 0;
        }

        public void ExitLocalCount()
        {
            localIndex = localCounters.Pop();
        }

        public TokenType GetType(string name)
        {
            int symbolIndex = symbolMap[name];
            return current.GetVariable(symbolIndex).Type;
        }

        int GetSymbolId(string name)
        {
            if (!symbolMap.TryGetValue(name, out int symbolIndex))
            {
                symbolIndex = symbolMap.Count;
                symbolMap.Add(name, symbolIndex);
            }

            return symbolIndex;
        }
    }
}
