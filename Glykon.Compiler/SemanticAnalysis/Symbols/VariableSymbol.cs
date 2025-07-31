using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.SemanticAnalysis.Symbols;

public class VariableSymbol(int id, TokenType type) : Symbol(id, type)
{
    public int LocalIndex { get; set; }
}
