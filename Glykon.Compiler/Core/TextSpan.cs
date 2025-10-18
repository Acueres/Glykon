namespace Glykon.Compiler.Core;

public readonly struct TextSpan(SourceText source, int start, int length)
{
    public SourceText Source { get; } = source;
    public int Start { get; } = start;
    public int Length { get; } = length;

    public ReadOnlySpan<char> AsSpan() => Source.Slice(this);

    public string Text => AsSpan().ToString();
}