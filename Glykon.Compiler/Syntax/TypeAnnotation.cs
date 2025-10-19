namespace Glykon.Compiler.Syntax;

public class TypeAnnotation(string name)
{
    public string Name { get; } = name;

    public static TypeAnnotation None { get;  } = new("none");
}
