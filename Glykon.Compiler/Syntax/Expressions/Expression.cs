﻿namespace Glykon.Compiler.Syntax.Expressions;

public enum ExpressionKind : byte
{
    Unary,
    Binary,
    Call,
    Grouping,
    Literal,
    Variable,
    Assignment,
    Logical
}

public abstract class Expression
{
    public abstract ExpressionKind Kind { get; }
}
