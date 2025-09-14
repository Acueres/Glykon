namespace Glykon.Compiler.Syntax.Expressions
{
    public class CallExpr(Expression callee, Token closingParenthesis, List<Expression> args) : Expression
    {
        public override ExpressionKind Kind => ExpressionKind.Call;
        public Expression Callee { get; } = callee;
        public Token ClosingParenthesis { get; } = closingParenthesis;
        public List<Expression> Args { get; } = args;
    }
}
