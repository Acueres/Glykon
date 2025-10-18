namespace Glykon.Compiler.Core;

public class SourceText(string fileName, string text)
{
    public string FileName { get; } = fileName;
    public ReadOnlyMemory<char> Buffer { get; } = text.AsMemory();
    public int Length => Buffer.Length;

    readonly string text = text;

    public ReadOnlySpan<char> Slice(TextSpan span) =>
        Buffer.Span.Slice(span.Start, span.Length);

    public ReadOnlySpan<char> Slice(int start, int length) =>
        Buffer.Span.Slice(start, length);

    public char this[int index]
    {
        get => text[index];
    }
}
