using System.Reflection;

namespace Glykon.Compiler.Backend.CIL;

public sealed record EmittedImage(
    string AssemblyName,
    Assembly Assembly,
    MethodInfo EntryPoint,
    FunctionInfo[] Functions);