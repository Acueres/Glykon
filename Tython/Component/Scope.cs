using Tython.Model;

namespace Tython.Component
{
    public class Scope
    {
        readonly Scope root;
        readonly Dictionary<string, int> symbols = [];
        readonly Dictionary<int, TokenType> symbolTypes = [];
        readonly HashSet<string> initialized = [];

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

        public int Add(string name, TokenType type)
        {
            int index = symbols.Count;
            symbols.Add(name, index);
            symbolTypes.Add(index, type);
            return index;
        }

        public void Initialize(string name)
        {
            initialized.Add(name);
        }

        public (int, TokenType) Get(string name, bool checkInitialization = false)
        {
            if (!symbols.TryGetValue(name, out int index)
                || (checkInitialization && root is not null && !initialized.Contains(name)))
            {
                if (root is null)
                {
                    return (-1, TokenType.Null);
                }

                return root.Get(name);
            }

            TokenType type = symbolTypes[index];
            return (index + Index, type);
        }
    }
}
