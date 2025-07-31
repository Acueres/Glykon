namespace Glykon.Compiler.Syntax.Expressions
{
    public class BinaryExpr(Token oper, IExpression left, IExpression right) : IExpression
    {
        public ExpressionType Type => ExpressionType.Binary;
        public Token Operator { get; } = oper;
        public IExpression Left { get; } = left;
        public IExpression Right { get; } = right;
    }
}
