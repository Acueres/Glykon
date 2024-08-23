using System;

namespace Tython.Model
{
    public class SymbolTable
    {
        readonly Dictionary<string, int> symbols = [];
        readonly Dictionary<int, TokenType> symbolTypes = [];

        public void Add(string name, TokenType type)
        {
            int index = symbols.Count;
            symbols.Add(name, index);
            symbolTypes.Add(index, type);
        }

        public (int, TokenType) Get(string name)
        {
            int index = symbols[name];
            TokenType type = symbolTypes[index];
            return (index, type);
        }

        public int GetIndex(string name) => symbols[name];
        public TokenType GetType(string name) => symbolTypes[symbols[name]];
    }
}
