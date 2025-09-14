using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class VariableDeclaration(Expression expr, string name, TokenKind varType) : Statement
    {
        public override StatementKind Kind => StatementKind.Variable;
        public Expression Expression { get; } = expr;
        public string Name { get; } = name;
        public TokenKind DeclaredType { get; } = varType;
    }
}
