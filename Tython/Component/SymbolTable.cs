using Tython.Model;

namespace Tython.Component
{
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

        public int Add(string name, TokenType type)
        {
            if (!symbolMap.TryGetValue(name, out int symbolIndex))
            {
                symbolIndex = symbolMap.Count;
                symbolMap.Add(name, symbolIndex);
            }

            int index = current.Add(symbolIndex, type);
            return index;
        }

        public (int, TokenType) Get(string name, bool checkInitialization)
        {
            int symbolIndex = symbolMap[name];
            (int index, TokenType type) = current.Get(symbolIndex, checkInitialization);
            return (index, type);
        }

        public void Initialize(string name)
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

        public void ResetScope() => current = global;

        public TokenType GetType(string name)
        {
            int symbolIndex = symbolMap[name];
            return current.Get(symbolIndex).Item2;
        }
    }
}
