using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Semantics
{
    public class SemanticRefiner(SymbolTable symbolTable, string fileName)
    {
        readonly SymbolTable st = symbolTable;

        readonly List<IGlykonError> errors = [];

        public void Refine(IStatement statement)
        {
            CheckUnenclosedJumpStatements(statement);

            if (statement is FunctionStmt fStmt)
            {
                NormalizeLocalFunction(fStmt);
            }
        }

        public List<IGlykonError> GetErrors() => errors;

        void NormalizeLocalFunction(FunctionStmt fStmt)
        {
            BlockStmt functionBody = fStmt.Body;
            st.EnterScope(functionBody.ScopeIndex);

            Span<FunctionStmt> localFunctions = fStmt.Body.Statements
                .Where(s => s.Type == StatementType.Function)
                .Select(s => (FunctionStmt)s).ToArray();

            Span<IStatement> statements = fStmt.Body.Statements
                .Where(s => s.Type != StatementType.Function).ToArray();

            foreach (var localFStmt in localFunctions)
            {
                string originalName = localFStmt.Name;
                localFStmt.Name = $"{fStmt.Name}.{localFStmt.Name}";

                localFStmt.Signature = st.RegisterFunction(localFStmt.Name, localFStmt.ReturnType, [.. localFStmt.Parameters.Select(p => p.Type)]);

                foreach (var s in localFStmt.Body.Statements)
                {
                    AdjustLocalFunctionIdentifiers(originalName, localFStmt.Name, s);
                }

                foreach (var s in statements)
                {
                    AdjustLocalFunctionIdentifiers(originalName, localFStmt.Name, s);
                }
            }

            st.ExitScope();
        }

        void AdjustLocalFunctionIdentifiers(string originalName, string adjustedName, IStatement statement)
        {
            if (statement is null) return;

            switch (statement.Type)
            {
                case StatementType.If:
                    {
                        var ifStmt = (IfStmt)statement;
                        AdjustLocalFunctionIdentifiers(originalName, adjustedName, ifStmt.Expression);
                        AdjustLocalFunctionIdentifiers(originalName, adjustedName, ifStmt.ThenStatement);
                        AdjustLocalFunctionIdentifiers(originalName, adjustedName, ifStmt.ElseStatement);
                        break;
                    }
                case StatementType.While:
                    {
                        var whileStmt = (WhileStmt)statement;
                        AdjustLocalFunctionIdentifiers(originalName, adjustedName, whileStmt.Expression);
                        AdjustLocalFunctionIdentifiers(originalName, adjustedName, whileStmt.Statement);
                        break;
                    }
                case StatementType.Block:
                    {
                        var blockStmt = (BlockStmt)statement;
                        st.EnterScope(blockStmt.ScopeIndex);
                        foreach (var stmt in blockStmt.Statements)
                        {
                            AdjustLocalFunctionIdentifiers(originalName, adjustedName, stmt);
                        }
                        st.ExitScope();
                        break;
                    }
                case StatementType.Function:
                    {
                        var fStmt = (FunctionStmt)statement;
                        NormalizeLocalFunction(fStmt);
                        break;
                    }
                default:
                    AdjustLocalFunctionIdentifiers(originalName, adjustedName, statement.Expression);
                    break;
            }
        }

        static void AdjustLocalFunctionIdentifiers(string originalName, string adjustedName, IExpression expression)
        {
            if (expression is null) return;

            if (expression is CallExpr callExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, callExpr.Callee);
                foreach (var arg in callExpr.Args)
                {
                    AdjustLocalFunctionIdentifiers(originalName, adjustedName, arg);
                }
            }
            else if (expression is VariableExpr varExpr)
            {
                if (varExpr.Name == originalName)
                {
                    varExpr.Name = adjustedName;
                }
            }
            else if (expression is AssignmentExpr assignmentExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, assignmentExpr.Right);
            }
            else if (expression is BinaryExpr binaryExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, binaryExpr.Left);
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, binaryExpr.Right);
            }
            else if (expression is UnaryExpr unaryExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, unaryExpr.Expression);
            }
            else if (expression is GroupingExpr groupingExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, groupingExpr.Expression);
            }
            else if (expression is LogicalExpr logicalExpr)
            {
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, logicalExpr.Left);
                AdjustLocalFunctionIdentifiers(originalName, adjustedName, logicalExpr.Right);
            }
        }

        void CheckUnenclosedJumpStatements(IStatement statement)
        {
            if (statement.Type == StatementType.While) return;

            if (statement is IfStmt ifStmt)
            {
                CheckUnenclosedJumpStatements(ifStmt.ThenStatement);

                if (ifStmt.ElseStatement is not null)
                {
                    CheckUnenclosedJumpStatements(ifStmt.ElseStatement);
                }
                return;
            }

            if (statement is BlockStmt blockStmt)
            {
                foreach (IStatement s in blockStmt.Statements)
                {
                    CheckUnenclosedJumpStatements(s);
                }

                return;
            }

            if (statement is JumpStmt jumpStmt)
            {
                ParseError error = new(jumpStmt.Token, fileName, "No enclosing loop out of which to break or continue");
                errors.Add(error);
            }
        }
    }
}
