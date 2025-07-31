using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Syntax.Expressions;

namespace Glykon.Compiler.Syntax.Statements
{
    public class FunctionStmt(string name, FunctionSymbol symbol, List<ParameterSymbol> parameters, TokenType returnType, BlockStmt body) : IStatement
    {
        public StatementType Type => StatementType.Function;
        public IExpression Expression { get; }
        public string Name { get; set; } = name;
        public FunctionSymbol Signature { get; set; } = symbol;
        public List<ParameterSymbol> Parameters { get; } = parameters;
        public TokenType ReturnType { get; } = returnType;
        public BlockStmt Body { get; } = body;
    }
}
