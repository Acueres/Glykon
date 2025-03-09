using TythonCompiler.SemanticAnalysis;
using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Tokenization;

namespace TythonCompiler.Syntax.Statements
{
    public class FunctionStmt(string name, FunctionSignature signature, int scopeIndex, List<Parameter> parameters, TokenType returnType, List<IStatement> body) : IStatement
    {
        public StatementType Type => StatementType.Function;
        public IExpression Expression { get; }
        public string Name { get; set; } = name;
        public FunctionSignature Signature { get; set; } = signature;
        public int ScopeIndex { get; } = scopeIndex;
        public List<Parameter> Parameters { get; } = parameters;
        public TokenType ReturnType { get; } = returnType;
        public List<IStatement> Body { get; } = body;
    }
}
