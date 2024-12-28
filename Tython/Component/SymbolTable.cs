using Tython.Model;

namespace Tython.Component
{
    public class SymbolTable
    {
        readonly Scope global = new();
        readonly List<Scope> scopes;
        readonly Dictionary<string, int> symbolMap = [];

        Scope current;
        int symbolCounter = 0;

        public SymbolTable()
        {
            scopes = [global];
            current = global;
        }

        public Symbol Add(string name, TokenType type)
        {
            if (!symbolMap.TryGetValue(name, out int symbolIndex))
            {
                symbolIndex = symbolMap.Count;
                symbolMap.Add(name, symbolIndex);
            }

            Symbol symbol = current.Add(symbolIndex, symbolCounter++, type);
            return symbol;
        }

        public Symbol Get(string name)
        {
            int symbolIndex = symbolMap[name];
            Symbol symbol = current.Get(symbolIndex);
            return symbol;
        }

        public Symbol GetInitialized(string name)
        {
            int symbolIndex = symbolMap[name];
            Symbol symbol = current.GetInitialized(symbolIndex);
            return symbol;
        }

        public void InitializeSymbol(string name)
        {
            int symbolIndex = symbolMap[name];
            current.Initialize(symbolIndex);
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

        public TokenType GetType(string name)
        {
            int symbolIndex = symbolMap[name];
            return current.Get(symbolIndex).Type;
        }
    }
}
