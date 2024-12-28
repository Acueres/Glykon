namespace Tython.Model
{
    public readonly struct Symbol(int index, int symbolId, TokenType type)
    {
        public int Index { get; } = index;
        public int SymbolId { get; } = symbolId;
        public TokenType Type { get; } = type;

        public static Symbol Null => new(-1, -1, TokenType.Null);
    }
}
