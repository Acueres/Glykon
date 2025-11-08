namespace Glykon.Compiler.Core;

public enum ConstantKind
{
    Int,
    Real,
    Bool,
    String,
    None
}

public readonly struct ConstantValue
{
    public ConstantKind Kind { get; }
    public long Int { get; }
    public double Real { get; }
    public bool Bool { get; }
    public string String { get; }
    public bool IsNone => Kind == ConstantKind.None;

    private ConstantValue(ConstantKind kind, long i = 0, double d = 0, bool b = false, string s = "")
    {
        Kind = kind;
        Int = i;
        Real = d;
        Bool = b;
        String = s;
    }

    public static ConstantValue FromInt(long v) => new(ConstantKind.Int, i: v);
    public static ConstantValue FromReal(double v) => new(ConstantKind.Real, d: v);
    public static ConstantValue FromBool(bool v) => new(ConstantKind.Bool, b: v);
    public static ConstantValue FromString(string v) => new(ConstantKind.String, s: v);
    public static ConstantValue None() => new(ConstantKind.None);
}
