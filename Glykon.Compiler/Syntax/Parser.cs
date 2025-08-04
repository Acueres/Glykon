using Glykon.Compiler.Semantics;
using Glykon.Compiler.Semantics.Symbols;
using Glykon.Compiler.Diagnostics.Exceptions;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Syntax;

public class Parser(Token[] tokens, IdentifierInterner interner, string filename)
{
    readonly string fileName = filename;

    bool AtEnd => tokenIndex >= tokens.Length;

    readonly Token[] tokens = tokens;
    readonly List<IStatement> statements = [];
    readonly SymbolTable symbolTable = new(interner);
    readonly List<IGlykonError> errors = [];
    int tokenIndex;

    public (IStatement[], SymbolTable symbolTable, List<IGlykonError>) Execute()
    {
        RegisterStd();
        
        while (!AtEnd)
        {
            try
            {
                if (Current.Kind == TokenType.EOF) break;
                IStatement stmt = ParseStatement();
                statements.Add(stmt);
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        return (statements.ToArray(), symbolTable, errors);
    }

    void RegisterStd()
    {
        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.String]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Int]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Real]);

        symbolTable.RegisterFunction("println", TokenType.None, [TokenType.Bool]);
    }

    IStatement ParseStatement()
    {
        if (Match(TokenType.Const))
        {
            return ParseConstantDeclaration();
        }

        if (Match(TokenType.Let))
        {
            return ParseVariableDeclarationStatement();
        }

        if (Match(TokenType.Return))
        {
            return ParseReturnStatement();
        }

        if (Match(TokenType.BraceLeft))
        {
            int scopeIndex = symbolTable.BeginScope(ScopeKind.Block);

            var blockStatement = ParseBlockStatement(scopeIndex);

            symbolTable.ExitScope();

            return blockStatement;
        }

        if (Match(TokenType.If))
        {
            return ParseIfStatement();
        }

        if (Match(TokenType.While))
        {
            return ParseWhileStatement();
        }

        if (Match(TokenType.Break, TokenType.Continue))
        {
            JumpStmt jumpStmt = new(Previous);

            TerminateStatement("Expect ';' after break or continue");
            return jumpStmt;
        }

        if (Match(TokenType.Def))
        {
            return ParseFunctionDeclaration();
        }

        IExpression expr = ParseExpression();

        TerminateStatement("Expect ';' after expression");

        return new ExpressionStmt(expr);
    }

    BlockStmt ParseBlockStatement(int scopeIndex)
    {
        List<IStatement> statements = [];

        while (Current.Kind != TokenType.BraceRight && !AtEnd)
        {
            IStatement stmt = ParseStatement();
            statements.Add(stmt);
        }

        Consume(TokenType.BraceRight, "Expect '}' after block");

        return new BlockStmt(statements, scopeIndex);
    }

    IfStmt ParseIfStatement()
    {
        IExpression condition = ParseExpression();

        // Handle ASI artefacts
        Match(TokenType.Semicolon);

        IStatement stmt;
        if (Match(TokenType.BraceLeft))
        {
            int scopeIndex = symbolTable.BeginScope(ScopeKind.Block);

            stmt = ParseBlockStatement(scopeIndex);

            symbolTable.ExitScope();
        }
        else
        {
            Consume(TokenType.Colon, "Expect ':' after if condition");

            stmt = ParseStatement();
        }

        IStatement? elseStmt = null;
        if (Match(TokenType.Else))
        {
            // Handle ASI artefacts
            Match(TokenType.Semicolon);
            elseStmt = ParseStatement();
        }
        else if (Match(TokenType.Elif))
        {
            // Handle ASI artefacts
            Match(TokenType.Semicolon);
            elseStmt = ParseIfStatement();
        }

        return new IfStmt(condition, stmt, elseStmt);
    }

    WhileStmt ParseWhileStatement()
    {
        IExpression condition = ParseExpression();

        // Handle ASI artefacts
        Match(TokenType.Semicolon);

        if (Match(TokenType.BraceLeft))
        {
            int scopeIndex = symbolTable.BeginScope(ScopeKind.Loop);

            var body = ParseBlockStatement(scopeIndex);

            symbolTable.ExitScope();

            return new WhileStmt(condition, body);
        }

        Consume(TokenType.Colon, "Expect ':' after while condition");

        var statement = ParseStatement();

        return new WhileStmt(condition, statement);
    }

    FunctionStmt ParseFunctionDeclaration()
    {
        Token functionName = Consume(TokenType.Identifier, "Expect function name");
        Consume(TokenType.ParenthesisLeft, "Expect '(' after function name");
        List<(string Name, TokenType Type)> parameters = [];

        if (Current.Kind != TokenType.ParenthesisRight)
        {
            parameters = ParseParameters();
        }

        Consume(TokenType.ParenthesisRight, "Expect ')' after parameters");

        TokenType returnType = TokenType.None;
        if (Match(TokenType.Arrow))
        {
            returnType = ParseTypeDeclaration();
        }

        var symbol = symbolTable.RegisterFunction(functionName.StringValue, returnType, [.. parameters.Select(p => p.Type)]);

        int scopeIndex = symbolTable.BeginScope(symbol);

        List<ParameterSymbol> parameterSymbols = new(parameters.Count);
        foreach (var parameter in parameters)
        {
            var parameterSymbol = symbolTable.RegisterParameter(parameter.Name, parameter.Type);
            parameterSymbols.Add(parameterSymbol);
        }

        Consume(TokenType.BraceLeft, "Expect '{' before function body");

        BlockStmt body = ParseBlockStatement(scopeIndex);

        symbolTable.ExitScope();

        return new FunctionStmt(functionName.StringValue, symbol, parameterSymbols, returnType, body);
    }

    ReturnStmt ParseReturnStatement()
    {
        if (Match(TokenType.Semicolon) || Current.Kind == TokenType.BraceRight)
        {
            return new ReturnStmt(null);
        }

        IExpression value = ParseExpression();

        TerminateStatement("Expect ';' after return value");

        return new ReturnStmt(value);
    }

    VariableStmt ParseVariableDeclarationStatement()
    {
        Token token = Consume(TokenType.Identifier, "Expect variable name");

        TokenType declaredType = TokenType.None;
        if (Match(TokenType.Colon))
        {
            declaredType = ParseTypeDeclaration();
        }

        IExpression? initializer = null;
        if (Match(TokenType.Assignment))
        {
            initializer = ParseExpression();
        }

        if (initializer == null)
        {
            ParseError error = new(token, fileName, "Variable must be initialized");
            errors.Add(error);
            throw error.Exception();
        }
        else
        {
            TerminateStatement("Expect ';' after variable declaration");

            string name = token.StringValue;
            symbolTable.RegisterVariable(name, declaredType);
            return new(initializer, name, declaredType);
        }
    }

    ConstantStmt ParseConstantDeclaration()
    {
        Token token = Consume(TokenType.Identifier, "Expect constant name");

        Consume(TokenType.Colon, "Expect type declaration");
        TokenType declaredType = ParseTypeDeclaration();

        Consume(TokenType.Assignment, "Expect constant value");
        IExpression initializer = ParseExpression();

        if (initializer is not LiteralExpr literal)
        {
            ParseError error = new(token, fileName, "Const value must be literal");
            errors.Add(error);
            throw error.Exception();
        }

        TerminateStatement("Expect ';' after constant declaration");

        string name = token.StringValue;
        symbolTable.RegisterConstant(name, literal.Token, declaredType);
        return new(initializer, name, declaredType);
    }

    TokenType ParseTypeDeclaration()
    {
        Token next = Advance();
        return next.Kind;
    }

    List<(string Name, TokenType Type)> ParseParameters()
    {
        List<(string Name, TokenType Type)> parameters = [];
        do
        {
            if (parameters.Count > ushort.MaxValue)
            {
                errors.Add(new ParseError(Current, fileName, "Argument count exceeded"));
            }

            Token name = Consume(TokenType.Identifier, "Expect parameter name");
            Consume(TokenType.Colon, "Expect colon before type declaration");
            Token type = Advance();

            var parameter = (name.StringValue, type.Kind);
            parameters.Add(parameter);
        }
        while (Match(TokenType.Comma));

        return parameters;
    }

    public IExpression ParseExpression()
    {
        return ParseAssignment();
    }

    IExpression ParseAssignment()
    {
        IExpression expr = ParseLogicalOr();

        if (Match(TokenType.Assignment))
        {
            Token token = Peek(-2);

            IExpression value = ParseAssignment();

            if (expr.Type == ExpressionType.Variable)
            {
                string name = ((VariableExpr)expr).Name;
                return new AssignmentExpr(name, value);
            }

            ParseError error = new(token, fileName, "Invalid assignment target");
            errors.Add(error);
        }

        return expr;
    }

    IExpression ParseLogicalOr()
    {
        IExpression expr = ParseLogicalAnd();

        while (Match(TokenType.Or))
        {
            Token oper = Previous;
            IExpression right = ParseLogicalAnd();
            expr = new LogicalExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseLogicalAnd()
    {
        IExpression expr = ParseEquality();

        while (Match(TokenType.And))
        {
            Token oper = Previous;
            IExpression right = ParseEquality();
            expr = new LogicalExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseEquality()
    {
        IExpression expr = ParseComparison();

        while (Match(TokenType.Equal, TokenType.NotEqual))
        {
            Token oper = Previous;
            IExpression right = ParseComparison();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseComparison()
    {
        IExpression expr = ParseTerm();

        while (Match(TokenType.Greater, TokenType.GreaterEqual, TokenType.Less, TokenType.LessEqual))
        {
            Token oper = Previous;
            IExpression right = ParseTerm();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseTerm()
    {
        IExpression expr = ParseFactor();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            Token oper = Previous;
            IExpression right = ParseFactor();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseFactor()
    {
        IExpression expr = ParseUnary();

        while (Match(TokenType.Slash, TokenType.Star))
        {
            Token oper = Previous;
            IExpression right = ParseUnary();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    IExpression ParseUnary()
    {
        if (Match(TokenType.Not, TokenType.Minus))
        {
            Token oper = Previous;
            IExpression right = ParseUnary();
            return new UnaryExpr(oper, right);
        }

        return ParseCall();
    }

    IExpression ParseCall()
    {
        IExpression expr = ParsePrimary();

        while (Match(TokenType.ParenthesisLeft))
        {
            expr = CompleteCall(expr);
        }

        return expr;
    }

    CallExpr CompleteCall(IExpression callee)
    {
        List<IExpression> args = [];
        if (Current.Kind != TokenType.ParenthesisRight)
        {
            do
            {
                if (args.Count > ushort.MaxValue)
                {
                    errors.Add(new ParseError(Current, fileName, "Argument count exceeded"));
                }

                args.Add(ParseExpression());
            }
            while (Match(TokenType.Comma));
        }
        Token closingParenthesis = Consume(TokenType.ParenthesisRight, "Expect ')' after arguments.");
        return new CallExpr(callee, closingParenthesis, args);
    }

    IExpression ParsePrimary()
    {
        if (Match(TokenType.None, TokenType.LiteralTrue, TokenType.LiteralFalse,
                  TokenType.LiteralInt, TokenType.LiteralReal, TokenType.LiteralString))
            return new LiteralExpr(Previous);

        if (Match(TokenType.Identifier))
        {
            string name = Previous.StringValue;
            return new VariableExpr(name);
        }

        if (Match(TokenType.ParenthesisLeft))
        {
            IExpression expr = ParseExpression();
            Consume(TokenType.ParenthesisRight, "Expect ')' after expression");
            return new GroupingExpr(expr);
        }

        ParseError error = new(Current, fileName, "Expect expression");
        errors.Add(error);
        throw error.Exception();
    }

    /// <summary>
    /// Handle cases where a semicolon is optional, before a '}'*/
    /// </summary>
    /// <param name="errorMessage"></param>
    void TerminateStatement(string errorMessage)
    {
        if (Current.Kind != TokenType.BraceRight)
        {
            Consume(TokenType.Semicolon, errorMessage);
        }
    }

    void Synchronize()
    {
        Advance();

        while (!AtEnd)
        {
            if (Current.Kind == TokenType.Semicolon) return;

            switch (Current.Kind)
            {
                case TokenType.Class:
                case TokenType.Struct:
                case TokenType.Interface:
                case TokenType.Enum:
                case TokenType.Def:
                case TokenType.For:
                case TokenType.If:
                case TokenType.While:
                case TokenType.Return:
                    return;
            }

            Advance();
        }
    }

    Token Consume(TokenType symbol, string message)
    {
        if (Current.Kind == symbol) return Advance();
        ParseError error = new(Current, fileName, message);
        errors.Add(error);
        throw error.Exception();
    }

    ref Token Advance()
    {
        return ref tokens[tokenIndex++];
    }

    ref readonly Token Peek(int offset)
    {
        int peekIndex = tokenIndex + offset;
        if (peekIndex < 0 || peekIndex >= tokens.Length)
        {
            return ref Token.Empty;
        }
        return ref tokens[peekIndex];
    }

    ref readonly Token Next => ref Peek(1);
    ref readonly Token Current => ref Peek(0);
    ref readonly Token Previous => ref Peek(-1);

    bool Match(params TokenType[] values)
    {
        foreach (TokenType value in values)
        {
            if (Current.Kind == value)
            {
                Advance();
                return true;
            }
        }

        return false;
    }
}
