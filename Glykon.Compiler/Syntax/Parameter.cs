namespace Glykon.Compiler.Syntax;

public class Parameter(string name, TypeAnnotation type)
{
    public string Name { get; } = name;
    public TypeAnnotation Type { get; } = type;
}