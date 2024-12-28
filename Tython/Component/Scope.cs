using Tython.Model;

namespace Tython.Component
{
    public class Scope
    {
        readonly Scope root;
        readonly Dictionary<int, Symbol> symbols = [];
        readonly HashSet<int> initialized = [];

        public Scope Root => root;
        public int ScopeIndex { get; }

        public Scope()
        {
            root = null;
            ScopeIndex = 0;
        }

        public Scope(Scope root, int scopeIndex)
        {
            this.root = root;
            ScopeIndex = scopeIndex;
        }

        public Symbol Add(int symbolId, int index, TokenType type)
        {
            Symbol symbol = new(index, symbolId, type);
            symbols.Add(symbolId, symbol);
            return symbol;
        }

        public void Initialize(int symbolId)
        {
            initialized.Add(symbolId);
        }

        /**Search for symbol in scopes disregarding its initialization status.*/
        public Symbol Get(int symbolId)
        {
            if (!symbols.TryGetValue(symbolId, out Symbol symbol))
            {
                if (root is null)
                {
                    return Symbol.Null;
                }

                return root.Get(symbolId);
            }

            return symbol;
        }

        /**Search for an initialized symbol in scopes.*/
        public Symbol GetInitialized(int symbolId)
        {
            if (!initialized.Contains(symbolId))
            {
                if (root is null)
                {
                    return Symbol.Null;
                }

                return root.GetInitialized(symbolId);
            }

            return symbols[symbolId];
        }
    }
}
