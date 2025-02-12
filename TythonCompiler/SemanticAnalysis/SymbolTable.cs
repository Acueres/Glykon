using TythonCompiler.SemanticAnalysis.Symbols;
using TythonCompiler.Tokenization;

namespace TythonCompiler.SemanticAnalysis
{
    public class SymbolTable
    {
        readonly Scope global = new();
        readonly List<Scope> scopes;
        readonly Dictionary<string, int> symbolMap = [];

        Scope current;

        public SymbolTable()
        {
            scopes = [global];
            current = global;
        }

        public FunctionSymbol RegisterFunction(string name, TokenType returnType, TokenType[] parameterTypes)
        {
            int symbolIndex = GetSymbolId(name);
            FunctionSymbol symbol = current.AddFunction(symbolIndex, returnType, parameterTypes);
            return symbol;
        }

        public FunctionSymbol? GetFunction(string name)
        {
            int symbolIndex = symbolMap[name];
            FunctionSymbol? function = current.GetFunction(symbolIndex);
            return function;
        }

        public ConstantSymbol RegisterConstant(string name, object value, TokenType type)
        {
            int symbolIndex = GetSymbolId(name);
            ConstantSymbol constant = current.AddConstant(symbolIndex, value, type);
            return constant;
        }

        public ConstantSymbol? GetConstant(string name)
        {
            int symbolIndex = symbolMap[name];
            ConstantSymbol? constant = current.GetConstant(symbolIndex);
            return constant;
        }

        public ParameterSymbol RegisterParameter(string name, TokenType type)
        {
            int symbolIndex = GetSymbolId(name);
            ParameterSymbol parameter = current.AddParameter(symbolIndex, type);
            return parameter;
        }

        public ParameterSymbol? GetParameter(string name)
        {
            int symbolIndex = symbolMap[name];
            ParameterSymbol? parameter = current.GetParameter(symbolIndex);
            return parameter;
        }

        public VariableSymbol RegisterVariable(string name, TokenType type)
        {
            int symbolIndex = GetSymbolId(name);
            VariableSymbol variable = current.AddVariable(symbolIndex, type);
            return variable;
        }

        public VariableSymbol? GetVariable(string name)
        {
            int symbolIndex = symbolMap[name];
            VariableSymbol? variable = current.GetVariable(symbolIndex);
            return variable;
        }

        public VariableSymbol? GetInitializedVariable(string name)
        {
            int symbolIndex = symbolMap[name];
            VariableSymbol? variable = current.GetInitializedVariable(symbolIndex);
            return variable;
        }

        public void Initializevariable(string name)
        {
            int symbolIndex = symbolMap[name];
            current.InitializeVariable(symbolIndex);
        }

        public int BeginScope()
        {
            int index = scopes.Count;
            current = new Scope(current, index);
            scopes.Add(current);
            return index;
        }

        public void ExitScope()
        {
            current = current.Root;
        }

        public void EnterScope(int index)
        {
            current = scopes[index];
        }

        public void ResetScope()
        {
            current = global;
        }

        public TokenType GetType(string name)
        {
            int symbolIndex = symbolMap[name];

            ParameterSymbol? parameter = current.GetParameter(symbolIndex);
            if (parameter != null)
            {
                return parameter.Type;
            }

            VariableSymbol? variableSymbol = current.GetVariable(symbolIndex);
            if (variableSymbol != null)
            {
                return variableSymbol.Type;
            }

            ConstantSymbol? constantSymbol = current.GetConstant(symbolIndex);
            if (constantSymbol != null)
            {
                return constantSymbol.Type;
            }

            return TokenType.None;
        }

        int GetSymbolId(string name)
        {
            if (!symbolMap.TryGetValue(name, out int symbolIndex))
            {
                symbolIndex = symbolMap.Count;
                symbolMap.Add(name, symbolIndex);
            }

            return symbolIndex;
        }
    }
}
