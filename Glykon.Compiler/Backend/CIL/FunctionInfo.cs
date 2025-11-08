using System.Reflection;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Backend.CIL;

public class FunctionInfo(FunctionSymbol symbol, MethodInfo method)
{
    public FunctionSymbol Symbol { get; } = symbol;
    public MethodInfo Method { get; } = method;
}