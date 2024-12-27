using Tython.Model;

namespace Tython.Component
{
    public class SymbolTable
    {
        readonly Scope global = new();
        readonly List<Scope> scopes;
        Scope current;

        public SymbolTable()
        {
            scopes = [global];
            current = global;
        }

        public int Add(string name, TokenType type)
        {
            int index = current.Add(name, type);
            return index;
        }

        public (int, TokenType) Get(string name, bool checkInitialization)
        {
            (int index, TokenType type) = current.Get(name, checkInitialization);
            return (index, type);
        }

        public void Initialize(string name)
        {
            current.Initialize(name);
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

        public TokenType GetType(string name) => current.Get(name).Item2;
    }
}
