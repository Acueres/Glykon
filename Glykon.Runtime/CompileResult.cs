using System.Reflection;
using System.Runtime.Loader;

using Glykon.Compiler.Backend.CIL;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Semantics.Analysis;

namespace Glykon.Runtime;

public sealed record CompileResult(
    Assembly? Assembly,
    SemanticResult Semantics,
    IGlykonError[] Errors,
    AssemblyLoadContext? LoadContext,
    FunctionInfo[] Functions);