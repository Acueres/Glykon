namespace Glykon.Compiler.Core;

public class SourceText(string fileName, string text)
{
    public string FileName { get; } = fileName;
    public int Length => Buffer.Length;

    private ReadOnlyMemory<char> Buffer { get; } = text.AsMemory();

    public ReadOnlySpan<char> Slice(TextSpan span) =>
        Buffer.Span.Slice(span.Start, span.Length);

    public ReadOnlySpan<char> Slice(int start, int length) =>
        Buffer.Span.Slice(start, length);

    public char this[int index]
    {
        get => text[index];
    }
}
