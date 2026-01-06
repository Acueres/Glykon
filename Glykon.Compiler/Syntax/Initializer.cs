using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax;

public class Initializer(string name, Expression value)
{
    public string Name { get; } = name;
    public Expression Value { get; } = value;
}