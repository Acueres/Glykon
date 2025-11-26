using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements;

public class ForStmt(VariableDeclaration iter, RangeExpr range, Statement body) : Statement
{
    public override StatementKind Kind => StatementKind.For;
    
    public VariableDeclaration Iterator { get; } = iter;
    public RangeExpr Range { get; } = range;
    public Statement Body { get; } = body;
}