using Glykon.Compiler.Tokenization;

namespace Glykon.Compiler.Syntax.Expressions
{
    public class CallExpr(IExpression callee, Token closingParenthesis, List<IExpression> args) : IExpression
    {
        public ExpressionType Type => ExpressionType.Call;
        public IExpression Callee { get; } = callee;
        public Token ClosingParenthesis { get; } = closingParenthesis;
        public List<IExpression> Args { get; } = args;
    }
}
