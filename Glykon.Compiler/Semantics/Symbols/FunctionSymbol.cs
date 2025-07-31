using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class FunctionSymbol(int id, TokenType returnType, TokenType[] parameters) : Symbol(id, returnType)
{
    public TokenType[] Parameters { get; } = parameters;

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionSymbol sb) return false;

        return Id == sb.Id && Type == sb.Type
        && Parameters.SequenceEqual(sb.Parameters);
    }

    public override int GetHashCode()
    {
        const int prime = 17;
        int hc = Parameters.Length;
        foreach (TokenType val in Parameters)
        {
            hc = unchecked(hc * prime + (int)val);
        }

        return HashCode.Combine(Id, Type, hc);
    }
}

