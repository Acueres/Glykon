using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.SemanticAnalysis.Symbols;

public abstract class Symbol(int id, TokenType type)
{
    public int Id { get; } = id;
    public TokenType Type { get; set; } = type;
}