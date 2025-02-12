using TythonCompiler.SemanticAnalysis.Symbols;
using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis
{
    public class Scope
    {
        public Scope Root => root;
        public int ScopeIndex { get; }

        readonly Scope root;
        readonly Dictionary<int, VariableSymbol> variables = [];
        readonly Dictionary<int, ParameterSymbol> parameters = [];
        readonly Dictionary<int, ConstantSymbol> constants = [];
        readonly Dictionary<int, FunctionSymbol> functions = [];

        readonly HashSet<int> initialized = [];

        int parameterCount = 0;

        public Scope()
        {
            root = null;
            ScopeIndex = 0;
        }

        public Scope(Scope root, int scopeIndex)
        {
            this.root = root;
            ScopeIndex = scopeIndex;
        }

        public FunctionSymbol AddFunction(int symbolId, TokenType returnType, TokenType[] parameterTypes)
        {
            FunctionSymbol symbol = new(returnType, parameterTypes);
            functions.Add(symbolId, symbol);
            return symbol;
        }

        public FunctionSymbol? GetFunction(int symbolId)
        {
            if (!functions.TryGetValue(symbolId, out FunctionSymbol? symbol))
            {
                if (root is null)
                {
                    return null;
                }

                return root.GetFunction(symbolId);
            }

            return symbol;
        }

        public ConstantSymbol AddConstant(int symbolId, object value, TokenType type)
        {
            ConstantSymbol symbol = new(value, type);
            constants.Add(symbolId, symbol);
            return symbol;
        }

        public ConstantSymbol? GetConstant(int symbolId)
        {
            if (!constants.TryGetValue(symbolId, out ConstantSymbol? symbol))
            {
                if (root is null)
                {
                    return null;
                }

                return root.GetConstant(symbolId);
            }

            return symbol;
        }

        public ParameterSymbol AddParameter(int symbolId, TokenType type)
        {
            ParameterSymbol symbol = new(parameterCount++, type);
            parameters.Add(symbolId, symbol);
            return symbol;
        }

        public ParameterSymbol GetParameter(int symbolId)
        {
            if (!parameters.TryGetValue(symbolId, out ParameterSymbol? symbol))
            {
                if (root is null)
                {
                    return null;
                }

                return root.GetParameter(symbolId);
            }

            return symbol;
        }

        public VariableSymbol AddVariable(int symbolId, TokenType type)
        {
            VariableSymbol symbol = new(type);
            variables.Add(symbolId, symbol);
            return symbol;
        }

        public void InitializeVariable(int symbolId)
        {
            initialized.Add(symbolId);
        }

        /**Search for symbol in scopes disregarding its initialization status.*/
        public VariableSymbol? GetVariable(int symbolId)
        {
            if (!variables.TryGetValue(symbolId, out VariableSymbol? symbol))
            {
                if (root is null)
                {
                    return null;
                }

                return root.GetVariable(symbolId);
            }

            return symbol;
        }

        /**Search for an initialized symbol in scopes.*/
        public VariableSymbol? GetInitializedVariable(int symbolId)
        {
            if (!initialized.Contains(symbolId))
            {
                if (root is null)
                {
                    return null;
                }

                return root.GetInitializedVariable(symbolId);
            }

            return variables[symbolId];
        }
    }
}
