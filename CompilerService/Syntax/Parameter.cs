using CompilerService.Tokenization;

namespace CompilerService.Syntax
{
    public readonly struct Parameter(string name, TokenType type)
    {
        public string Name { get; } = name;
        public TokenType Type { get; } = type;
    }
}
