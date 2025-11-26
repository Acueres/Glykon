using Glykon.Compiler.Semantics.IR.Expressions;
using Glykon.Compiler.Semantics.Symbols;

namespace Glykon.Compiler.Semantics.IR.Statements;

public class IRForStmt(IRVariableDeclaration iter, IRRangeExpr range, IRStatement body) : IRStatement
{
    public override IRStatementKind Kind => IRStatementKind.For;
    
    public IRVariableDeclaration Iterator { get; } = iter;
    public IRRangeExpr Range { get; } = range;
    public IRStatement Body { get; } = body;
}