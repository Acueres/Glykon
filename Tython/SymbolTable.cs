namespace Tython
{
    public class SymbolTable
    {
        readonly Dictionary<string, int> symbols = [];

        public void Add(string name)
        {
            symbols.Add(name, symbols.Count);
        }

        public int Get(string name) => symbols[name];
    }
}
