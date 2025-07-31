using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.SemanticAnalysis.Symbols;

public class ConstantSymbol(int id, TokenType type, object value) : Symbol(id, type)
{
    public object Value { get; } = value;
}

