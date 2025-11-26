using System.Reflection;
using System.Reflection.Emit;

using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Backend.CIL;

public class CilEmitContext
{
    public Dictionary<FunctionSymbol, MethodInfo> Functions { get; init; }
    public Label? ReturnLabel { get; init; }
    public LocalBuilder? ReturnLocal { get; init; }
}