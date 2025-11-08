using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRReturnStmt(IRExpression? value, Token token) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.Return;
    public IRExpression? Value { get; } = value;
    public Token Token { get; } = token;
}
