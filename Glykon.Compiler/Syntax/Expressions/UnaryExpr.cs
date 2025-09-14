namespace Glykon.Compiler.Syntax.Expressions
{
    public class UnaryExpr(Token oper, Expression expr) : Expression
    {
        public override ExpressionKind Kind => ExpressionKind.Unary;
        public Token Operator { get; } = oper;
        public Expression Operand { get; } = expr;
    }
}
