namespace Glykon.Runtime;

public sealed record ExecutionResult(object? ReturnValue, string Stdout, Exception? Exception);