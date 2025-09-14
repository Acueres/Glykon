using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Symbols;

public class FunctionSymbol(int id, int qualifiedId, TokenKind returnType, TokenKind[] parameters) : Symbol(id, returnType)
{
    public int QualifiedId { get; } = qualifiedId;
    public TokenKind[] Parameters { get; } = parameters;

    public override bool Equals(object? obj)
    {
        if (obj is not FunctionSymbol sb) return false;

        return Id == sb.Id && QualifiedId == sb.QualifiedId && Type == sb.Type
        && Parameters.SequenceEqual(sb.Parameters);
    }

    public override int GetHashCode()
    {
        const int prime = 17;
        int hc = Parameters.Length;
        foreach (TokenKind val in Parameters)
        {
            hc = unchecked(hc * prime + (int)val);
        }

        return HashCode.Combine(Id, QualifiedId, Type, hc);
    }
}

