using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.Operators;

public static class TokenOpMap
{
    public static BinaryOp ToBinaryOp(TokenKind tk) => tk switch
    {
        TokenKind.Plus      => BinaryOp.Add,
        TokenKind.Minus     => BinaryOp.Subtract,
        TokenKind.Star      => BinaryOp.Multiply,
        TokenKind.Slash     => BinaryOp.Divide,
        TokenKind.Less      => BinaryOp.Less,
        TokenKind.LessEqual => BinaryOp.LessOrEqual,
        TokenKind.Greater   => BinaryOp.Greater,
        TokenKind.GreaterEqual => BinaryOp.GreaterOrEqual,
        TokenKind.Equal     => BinaryOp.Equal,
        TokenKind.NotEqual  => BinaryOp.NotEqual,
        TokenKind.And       => BinaryOp.LogicalAnd,
        TokenKind.Or        => BinaryOp.LogicalOr
    };

    public static UnaryOp ToUnaryOp(TokenKind tk) => tk switch
    {
        TokenKind.Minus  => UnaryOp.Minus,
        TokenKind.Not   => UnaryOp.LogicalNot
    };
}