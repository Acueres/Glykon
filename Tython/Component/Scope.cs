using Tython.Model;

namespace Tython.Component
{
    public class Scope
    {
        readonly Scope root;
        readonly Dictionary<int, int> symbols = [];
        readonly Dictionary<int, TokenType> symbolTypes = [];
        readonly HashSet<int> initialized = [];

        public Scope Root => root;
        public int Index { get; }

        public Scope()
        {
            root = null;
            Index = 0;
        }

        public Scope(Scope root, int index)
        {
            this.root = root;
            Index = index;
        }

        public int Add(int symbolId, TokenType type)
        {
            int index = symbols.Count;
            symbols.Add(symbolId, index);
            symbolTypes.Add(index, type);
            return index;
        }

        public void Initialize(int symbolId)
        {
            initialized.Add(symbolId);
        }

        public (int, TokenType) Get(int symbolId, bool checkInitialization = false)
        {
            if (!symbols.TryGetValue(symbolId, out int index)
                || (checkInitialization && root is not null && !initialized.Contains(symbolId)))
            {
                if (root is null)
                {
                    return (-1, TokenType.Null);
                }

                return root.Get(symbolId);
            }

            TokenType type = symbolTypes[index];
            return (index + Index, type);
        }
    }
}
