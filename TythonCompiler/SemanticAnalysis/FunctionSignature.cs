using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis;

public class FunctionSignature(int symbolId, TokenType[] parameterTypes)
{
    public int SymbolId { get; } = symbolId;
    public TokenType[] ParameterTypes { get; } = parameterTypes;

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionSignature sg) return false;

        return SymbolId == sg.SymbolId
        && ParameterTypes.SequenceEqual(sg.ParameterTypes);
    }

    public override int GetHashCode()
    {
        const int prime = 17;
        int hc = ParameterTypes.Length;
        foreach (TokenType val in ParameterTypes)
        {
            hc = unchecked(hc * prime + (int)val);
        }

        return HashCode.Combine(SymbolId, hc);
    }
}