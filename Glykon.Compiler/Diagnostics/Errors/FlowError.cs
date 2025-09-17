using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Diagnostics.Errors;

public sealed class FlowError(string fileName, string message, Token token) : IGlykonError
{
    public string FileName { get; } = fileName;
    public Token Token { get; } = token;
    public string Message { get; } = message;

    public void Report()
    {
        Console.WriteLine($"{Message} ({FileName}, line {Token.Line})");
    }
}
