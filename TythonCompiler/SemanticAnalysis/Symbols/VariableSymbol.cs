using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis.Symbols;

public class VariableSymbol(int id, TokenType type) : Symbol(id, type)
{
    public int LocalIndex { get; set; }
}
