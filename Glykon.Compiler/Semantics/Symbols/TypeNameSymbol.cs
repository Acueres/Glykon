using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Symbols;

public class TypeNameSymbol(int nameId, TypeSymbol type) : Symbol(nameId, type) { }