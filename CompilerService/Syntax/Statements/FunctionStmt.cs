using CompilerService.SemanticAnalysis.Symbols;
using CompilerService.Syntax.Expressions;
using CompilerService.Tokenization;

namespace CompilerService.Syntax.Statements
{
    public class FunctionStmt(string name, FunctionSymbol symbol, List<Parameter> parameters, TokenType returnType, BlockStmt body) : IStatement
    {
        public StatementType Type => StatementType.Function;
        public IExpression Expression { get; }
        public string Name { get; set; } = name;
        public FunctionSymbol Signature { get; set; } = symbol;
        public List<Parameter> Parameters { get; } = parameters;
        public TokenType ReturnType { get; } = returnType;
        public BlockStmt Body { get; } = body;
    }
}
