using Glykon.Compiler.Core;
using Glykon.Compiler.Semantics.Binding;
using Glykon.Compiler.Semantics.IR;
using Glykon.Compiler.Semantics.IR.Statements;
using Glykon.Compiler.Semantics.Rewriting;
using Glykon.Compiler.Semantics.Types;

namespace Glykon.Compiler.Semantics.Lowering;

public class Lowerer(IRTree ir, IdentifierInterner interner, TypeSystem ts, SymbolTable st, LanguageMode mode) : IRTreeRewriter
{
    public IRTree Lower()
    {
        List<IRStatement> wrapped = new(ir.Length);

        wrapped = mode == LanguageMode.Script ? WrapScript() : ir.Select(s => s).ToList();
        
        List<IRStatement> rewritten = new(wrapped.Count);
        
        rewritten.AddRange(wrapped.Select(VisitStmt));
        
        return new IRTree([..rewritten], ir.FileName);
    }

    private List<IRStatement> WrapScript()
    {
        List<IRStatement> functions = [];
        List<IRStatement> constants = [];
        List<IRStatement> scriptStatements = [];

        foreach (var stmt in ir)
        {
            if (stmt is IRFunctionDeclaration f)
            {
                functions.Add(f);
            }
            else if (stmt is IRConstantDeclaration c)
            {
                constants.Add(c);
            }
            else
            {
                scriptStatements.Add(stmt);
            }
        }

        var existingMain = functions
            .OfType<IRFunctionDeclaration>()
            .FirstOrDefault(f => interner[f.Signature.NameId] == "main");

        if (existingMain != null)
        {
            throw new InvalidOperationException(
                "Script mode cannot contain both top-level statements and a 'main' function definition.");
        }
        
        st.ResetScope();
        var mainSymbol = st.RegisterFunction("main", ts[TypeKind.None], []);
        if (mainSymbol == null)
        {
            throw new InvalidOperationException("Failed to register synthetic main function.");
        }
        
        var bodyBlock = new IRBlockStmt([..scriptStatements], st.GetCurrentScope());
        var mainDeclaration = new IRFunctionDeclaration(mainSymbol, [], mainSymbol.Type, bodyBlock);
        
        functions.Add(mainDeclaration);
        
        return [..constants, ..functions];
    }
}