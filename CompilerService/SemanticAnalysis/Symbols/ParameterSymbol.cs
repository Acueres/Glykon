using CompilerService.Tokenization;

namespace CompilerService.SemanticAnalysis.Symbols
{
    public class ParameterSymbol(int id, TokenType type, int index) : Symbol(id, type)
    {
        public int Index { get; } = index;
    }
}
