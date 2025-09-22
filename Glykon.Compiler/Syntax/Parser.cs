using Glykon.Compiler.Diagnostics.Exceptions;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;

namespace Glykon.Compiler.Syntax;

public class Parser(Token[] tokens, string filename)
{
    readonly string fileName = filename;

    bool AtEnd => tokenIndex >= tokens.Length;

    readonly Token[] tokens = tokens;
    readonly List<Statement> statements = [];
    readonly List<IGlykonError> errors = [];
    int tokenIndex;

    public (SyntaxTree, List<IGlykonError>) Execute()
    {
        while (!AtEnd)
        {
            try
            {
                if (Current.Kind == TokenKind.EOF) break;
                Statement stmt = ParseStatement();
                statements.Add(stmt);
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }
        var syntaxTree = new SyntaxTree([..statements], fileName);
        return (syntaxTree, errors);
    }

    Statement ParseStatement()
    {
        if (Match(TokenKind.Const))
        {
            return ParseConstantDeclaration();
        }

        if (Match(TokenKind.Let))
        {
            return ParseVariableDeclarationStatement();
        }

        if (Match(TokenKind.Return))
        {
            return ParseReturnStatement();
        }

        if (Match(TokenKind.BraceLeft))
        {
            var blockStatement = ParseBlockStatement();
            return blockStatement;
        }

        if (Match(TokenKind.If))
        {
            return ParseIfStatement();
        }

        if (Match(TokenKind.While))
        {
            return ParseWhileStatement();
        }

        if (Match(TokenKind.Break, TokenKind.Continue))
        {
            JumpStmt jumpStmt = new(Previous);

            TerminateStatement("Expect ';' after break or continue");
            return jumpStmt;
        }

        if (Match(TokenKind.Def))
        {
            return ParseFunctionDeclaration();
        }

        Expression expr = ParseExpression();

        TerminateStatement("Expect ';' after expression");

        return new ExpressionStmt(expr);
    }

    BlockStmt ParseBlockStatement()
    {
        List<Statement> statements = [];

        while (Current.Kind != TokenKind.BraceRight && !AtEnd)
        {
            Statement stmt = ParseStatement();
            statements.Add(stmt);
        }

        Consume(TokenKind.BraceRight, "Expect '}' after block");

        return new BlockStmt(statements);
    }

    IfStmt ParseIfStatement()
    {
        Expression condition = ParseLogicalOr();

        // Handle ASI artefacts
        Match(TokenKind.Semicolon);

        Statement stmt;
        if (Match(TokenKind.BraceLeft))
        {
            stmt = ParseBlockStatement();
        }
        else
        {
            Consume(TokenKind.Colon, "Expect ':' after if condition");

            stmt = ParseStatement();
        }

        Statement? elseStmt = null;
        if (Match(TokenKind.Else))
        {
            // Handle ASI artefacts
            Match(TokenKind.Semicolon);
            elseStmt = ParseStatement();
        }
        else if (Match(TokenKind.Elif))
        {
            // Handle ASI artefacts
            Match(TokenKind.Semicolon);
            elseStmt = ParseIfStatement();
        }

        return new IfStmt(condition, stmt, elseStmt);
    }

    WhileStmt ParseWhileStatement()
    {
        Expression condition = ParseLogicalOr();

        // Handle ASI artefacts
        Match(TokenKind.Semicolon);

        if (Match(TokenKind.BraceLeft))
        {
            var body = ParseBlockStatement();
            return new WhileStmt(condition, body);
        }

        Consume(TokenKind.Colon, "Expect ':' after while condition");

        var statement = ParseStatement();

        return new WhileStmt(condition, statement);
    }

    FunctionDeclaration ParseFunctionDeclaration()
    {
        Token functionName = Consume(TokenKind.Identifier, "Expect function name");
        Consume(TokenKind.ParenthesisLeft, "Expect '(' after function name");
        List<(string Name, TokenKind Type)> parameters = [];

        if (Current.Kind != TokenKind.ParenthesisRight)
        {
            parameters = ParseParameters();
        }

        Consume(TokenKind.ParenthesisRight, "Expect ')' after parameters");

        TokenKind returnType = TokenKind.None;
        if (Match(TokenKind.Arrow))
        {
            returnType = ParseTypeDeclaration();
        }

        BlockStmt body;
        if (Match(TokenKind.BraceLeft))
        {
            body = ParseBlockStatement();
        }
        else
        {
            Consume(TokenKind.Colon, "Body must be declared");
            var stmt = ParseStatement();
            body = new BlockStmt([stmt]);
        }

        return new FunctionDeclaration(functionName.StringValue, parameters, returnType, body);
    }

    ReturnStmt ParseReturnStatement()
    {
        Token token = Previous;
        if (Match(TokenKind.Semicolon) || Current.Kind == TokenKind.BraceRight)
        {
            return new ReturnStmt(null, token);
        }

        Expression value = ParseLogicalOr();

        TerminateStatement("Expect ';' after return value");

        return new ReturnStmt(value, token);
    }

    VariableDeclaration ParseVariableDeclarationStatement()
    {
        Token token = Consume(TokenKind.Identifier, "Expect variable name");

        TokenKind declaredType = TokenKind.None;
        if (Match(TokenKind.Colon))
        {
            declaredType = ParseTypeDeclaration();
        }

        Expression? initializer = null;
        if (Match(TokenKind.Assignment))
        {
            initializer = ParseLogicalOr();
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
            return new(initializer, name, declaredType);
        }
    }

    ConstantDeclaration ParseConstantDeclaration()
    {
        Token token = Consume(TokenKind.Identifier, "Expect constant name");

        Consume(TokenKind.Colon, "Expect type declaration");
        TokenKind declaredType = ParseTypeDeclaration();

        Consume(TokenKind.Assignment, "Expect constant value");
        Expression initializer = ParseExpression();

        TerminateStatement("Expect ';' after constant declaration");

        string name = token.StringValue;
        return new(initializer, name, declaredType);
    }

    TokenKind ParseTypeDeclaration()
    {
        Token next = Advance();
        return next.Kind;
    }

    List<(string Name, TokenKind Type)> ParseParameters()
    {
        List<(string Name, TokenKind Type)> parameters = [];
        do
        {
            if (parameters.Count > ushort.MaxValue)
            {
                errors.Add(new ParseError(Current, fileName, "Argument count exceeded"));
            }

            Token name = Consume(TokenKind.Identifier, "Expect parameter name");
            Consume(TokenKind.Colon, "Expect colon before type declaration");
            Token type = Advance();

            var parameter = (name.StringValue, type.Kind);
            parameters.Add(parameter);
        }
        while (Match(TokenKind.Comma));

        return parameters;
    }

    public Expression ParseExpression()
    {
        return ParseAssignment();
    }

    Expression ParseAssignment()
    {
        Expression expr = ParseLogicalOr();

        if (Match(TokenKind.Assignment))
        {
            Token token = Peek(-2);

            Expression value = ParseAssignment();

            if (expr.Kind == ExpressionKind.Variable)
            {
                string name = ((VariableExpr)expr).Name;
                return new AssignmentExpr(name, value);
            }

            ParseError error = new(token, fileName, "Invalid assignment target");
            errors.Add(error);
        }

        return expr;
    }

    Expression ParseLogicalOr()
    {
        Expression expr = ParseLogicalAnd();

        while (Match(TokenKind.Or))
        {
            Token oper = Previous;
            Expression right = ParseLogicalAnd();
            expr = new LogicalExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseLogicalAnd()
    {
        Expression expr = ParseEquality();

        while (Match(TokenKind.And))
        {
            Token oper = Previous;
            Expression right = ParseEquality();
            expr = new LogicalExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseEquality()
    {
        Expression expr = ParseComparison();

        while (Match(TokenKind.Equal, TokenKind.NotEqual))
        {
            Token oper = Previous;
            Expression right = ParseComparison();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseComparison()
    {
        Expression expr = ParseTerm();

        while (Match(TokenKind.Greater, TokenKind.GreaterEqual, TokenKind.Less, TokenKind.LessEqual))
        {
            Token oper = Previous;
            Expression right = ParseTerm();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseTerm()
    {
        Expression expr = ParseFactor();

        while (Match(TokenKind.Plus, TokenKind.Minus))
        {
            Token oper = Previous;
            Expression right = ParseFactor();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseFactor()
    {
        Expression expr = ParseUnary();

        while (Match(TokenKind.Slash, TokenKind.Star))
        {
            Token oper = Previous;
            Expression right = ParseUnary();
            expr = new BinaryExpr(oper, expr, right);
        }

        return expr;
    }

    Expression ParseUnary()
    {
        if (Match(TokenKind.Not, TokenKind.Minus))
        {
            Token oper = Previous;
            Expression right = ParseUnary();
            return new UnaryExpr(oper, right);
        }

        return ParseCall();
    }

    Expression ParseCall()
    {
        Expression expr = ParsePrimary();

        while (Match(TokenKind.ParenthesisLeft))
        {
            expr = CompleteCall(expr);
        }

        return expr;
    }

    CallExpr CompleteCall(Expression callee)
    {
        List<Expression> args = [];
        if (Current.Kind != TokenKind.ParenthesisRight)
        {
            do
            {
                if (args.Count > ushort.MaxValue)
                {
                    errors.Add(new ParseError(Current, fileName, "Argument count exceeded"));
                }

                args.Add(ParseExpression());
            }
            while (Match(TokenKind.Comma));
        }
        Token closingParenthesis = Consume(TokenKind.ParenthesisRight, "Expect ')' after arguments.");
        return new CallExpr(callee, closingParenthesis, args);
    }

    Expression ParsePrimary()
    {
        if (Match(TokenKind.None, TokenKind.LiteralTrue, TokenKind.LiteralFalse,
                  TokenKind.LiteralInt, TokenKind.LiteralReal, TokenKind.LiteralString))
            return new LiteralExpr(Previous);

        if (Match(TokenKind.Identifier))
        {
            string name = Previous.StringValue;
            return new VariableExpr(name);
        }

        if (Match(TokenKind.ParenthesisLeft))
        {
            Expression expr = ParseExpression();
            Consume(TokenKind.ParenthesisRight, "Expect ')' after expression");
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
        if (Current.Kind != TokenKind.BraceRight)
        {
            Consume(TokenKind.Semicolon, errorMessage);
        }
    }

    void Synchronize()
    {
        Advance();

        while (!AtEnd)
        {
            if (Current.Kind == TokenKind.Semicolon) return;

            switch (Current.Kind)
            {
                case TokenKind.Class:
                case TokenKind.Struct:
                case TokenKind.Interface:
                case TokenKind.Enum:
                case TokenKind.Def:
                case TokenKind.For:
                case TokenKind.If:
                case TokenKind.While:
                case TokenKind.Return:
                    return;
            }

            Advance();
        }
    }

    Token Consume(TokenKind symbol, string message)
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

    bool Match(params TokenKind[] values)
    {
        foreach (TokenKind value in values)
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
