namespace Glykon.Compiler.Syntax.Expressions
{
    public class LiteralExpr(Token token) : Expression
    {
        public override ExpressionKind Kind => ExpressionKind.Literal;
        public Token Token { get; } = token;
    }
}
