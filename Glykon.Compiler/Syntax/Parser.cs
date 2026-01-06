using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;
using Glykon.Compiler.Diagnostics.Exceptions;
using Glykon.Compiler.Syntax.Expressions;
using Glykon.Compiler.Syntax.Statements;
using System.Globalization;

namespace Glykon.Compiler.Syntax;

public class Parser(LexResult lexResult, string filename)
{
    private bool AtEnd => tokenIndex >= tokens.Length;

    private readonly Token[] tokens = lexResult.Tokens;
    private readonly List<Statement> statements = [];
    private readonly List<IGlykonError> errors = [];
    private int tokenIndex;

    public ParseResult Parse()
    {
        while (!AtEnd)
        {
            try
            {
                if (Current.Kind == TokenKind.EOF) break;
                Statement stmt = ParseDeclaration();
                statements.Add(stmt);
            }
            catch (ParseException)
            {
                Synchronize();
            }
        }

        var syntaxTree = new SyntaxTree([..statements], filename);
        return new ParseResult(syntaxTree, lexResult.Tokens, lexResult.Errors, [..errors]);
    }

    private Statement ParseDeclaration()
    {
        if (Match(TokenKind.Const))
        {
            return ParseConstantDeclaration();
        }

        if (Match(TokenKind.Let))
        {
            return ParseVariableDeclaration();
        }

        if (Match(TokenKind.Def))
        {
            return ParseFunctionDeclaration();
        }

        if (Match(TokenKind.Class))
        {
            return ParseClassDeclaration();
        }

        return ParseStatement();
    }

    private ClassDeclaration ParseClassDeclaration()
    {
        Token className = Consume(TokenKind.Identifier, "Expect class name");
        Consume(TokenKind.BraceLeft, "Body must be declared");

        List<MethodDeclaration> methods = [];
        List<FieldDeclaration> fields = [];
        List<ConstantDeclaration> constants = [];
        List<ClassDeclaration> nested = [];

        while (Current.Kind != TokenKind.BraceRight && !AtEnd)
        {
            if (Match(TokenKind.Def))
            {
                var method = ParseMethodDeclaration();
                methods.Add(method);
            }
            else if (Match(TokenKind.Const))
            {
                var constant = ParseConstantDeclaration();
                constants.Add(constant);
            }
            else if (Match(TokenKind.Class))
            {
                var classDecl =  ParseClassDeclaration();
                nested.Add(classDecl);
            }
            else
            {
                var field = ParseFieldDeclaration();
                fields.Add(field);
            }
        }

        Consume(TokenKind.BraceRight, "Expect '}' after class body");

        return new ClassDeclaration(className.Text, [..methods], [..fields], [..constants], [..nested]);
    }

    private MethodDeclaration ParseMethodDeclaration()
    {
        var func = ParseFunctionDeclaration(isMethod: true);
        bool isStatic = func.Parameters.Length == 0 || func.Parameters.First().Type != TypeAnnotation.None;
        return new MethodDeclaration(
            func.Name,
            func.Parameters,
            func.ReturnType,
            func.Body,
            isStatic
        );
    }

    private FieldDeclaration ParseFieldDeclaration()
    {
        Token identifierToken = Consume(TokenKind.Identifier, "Expect field name");
        
        Consume(TokenKind.Colon, "Expect type declaration");
        var declaredType = ParseTypeDeclaration();

        Expression? initializer = null;
        if (Match(TokenKind.Assignment))
        {
            initializer = ParseLogicalOr();
        }

        TerminateStatement("Expect ';' after field declaration", initializer);

        string name = identifierToken.Text;
        return new FieldDeclaration(initializer, name, declaredType);
    }

    private FunctionDeclaration ParseFunctionDeclaration(bool isMethod = false)
    {
        string name = isMethod ? "method" : "function";
        Token functionName = Consume(TokenKind.Identifier, $"Expect {name} name");
        Consume(TokenKind.ParenthesisLeft, $"Expect '(' after {name} name");
        List<Parameter> parameters = [];

        if (Current.Kind != TokenKind.ParenthesisRight)
        {
            parameters = ParseParameters(isMethod);
        }

        Consume(TokenKind.ParenthesisRight, "Expect ')' after parameters");

        TypeAnnotation returnType = TypeAnnotation.None;
        if (Match(TokenKind.Arrow))
        {
            returnType = ParseTypeDeclaration();
        }

        Consume(TokenKind.BraceLeft, "Body must be declared");
        
        BlockStmt body = ParseBlockStatement();

        return new FunctionDeclaration(functionName.Text, [..parameters], returnType, body);
    }

    private VariableDeclaration ParseVariableDeclaration()
    {
        bool immutable = Match(TokenKind.Const);
        
        Token identifierToken = Consume(TokenKind.Identifier, "Expect variable name");

        TypeAnnotation declaredType = TypeAnnotation.None;
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
            ParseError error = new(identifierToken, filename, "Variable must be initialized");
            errors.Add(error);
            throw error.Exception();
        }

        TerminateStatement("Expect ';' after variable declaration", initializer);

        string name = identifierToken.Text;
        return new VariableDeclaration(initializer, name, declaredType, immutable);
    }

    private ConstantDeclaration ParseConstantDeclaration()
    {
        Token token = Consume(TokenKind.Identifier, "Expect constant name");

        Consume(TokenKind.Colon, "Expect type declaration");

        TypeAnnotation declaredType = ParseTypeDeclaration();

        Consume(TokenKind.Assignment, "Expect constant value");
        Expression initializer = ParseLogicalOr();

        TerminateStatement("Expect ';' after constant declaration", initializer);

        string name = token.Text;
        return new(initializer, name, declaredType);
    }

    private List<Parameter> ParseParameters(bool methodParams = false)
    {
        List<Parameter> parameters = [];
        do
        {
            if (parameters.Count > ushort.MaxValue)
            {
                errors.Add(new ParseError(Current, filename, "Argument count exceeded"));
            }

            Token name = Consume(TokenKind.Identifier, "Expect parameter name");

            Parameter parameter;
            // Allow for self reference in methods
            if (methodParams && parameters.Count == 0 && Current.Kind != TokenKind.Colon)
            {
                parameter = new Parameter(name.Text, TypeAnnotation.None);
            }
            else
            {
                Consume(TokenKind.Colon, "Expect colon before type declaration");
                var type = ParseTypeDeclaration();

                parameter = new Parameter(name.Text, type);
            }

            parameters.Add(parameter);
        }
        while (Match(TokenKind.Comma) && !AtEnd);

        return parameters;
    }

    private Statement ParseStatement()
    {
        if (Match(TokenKind.Return))
        {
            return ParseReturnStatement();
        }

        if (Match(TokenKind.BraceLeft))
        {
            return ParseBlockStatement();
        }

        if (Match(TokenKind.If))
        {
            return ParseIfStatement();
        }

        if (Match(TokenKind.While))
        {
            return ParseWhileStatement();
        }
        
        if (Match(TokenKind.For))
        {
            return ParseForStatement();
        }

        if (Match(TokenKind.Break, TokenKind.Continue))
        {
            JumpStmt jumpStmt = new(Previous);

            TerminateStatement("Expect ';' after break or continue");
            return jumpStmt;
        }

        Expression expr = ParseExpression();

        TerminateStatement("Expect ';' after expression", expr);

        return new ExpressionStmt(expr);
    }

    private BlockStmt ParseBlockStatement()
    {
        List<Statement> stmts = [];

        while (Current.Kind != TokenKind.BraceRight && !AtEnd)
        {
            Statement stmt = ParseDeclaration();
            stmts.Add(stmt);
        }

        Consume(TokenKind.BraceRight, "Expect '}' after block");

        return new BlockStmt(stmts);
    }

    private IfStmt ParseIfStatement()
    {
        Expression condition = ParseLogicalOr();

        // Handle ASI artifacts
        Match(TokenKind.Semicolon);
        
        Consume(TokenKind.BraceLeft, "Expect '{' after if condition");
        
        Statement body = ParseBlockStatement();

        Statement? elseStmt = null;
        if (Match(TokenKind.Else))
        {
            // Handle ASI artifacts
            Match(TokenKind.Semicolon);
            elseStmt = ParseStatement();
        }
        else if (Match(TokenKind.Elif))
        {
            // Handle ASI artifacts
            Match(TokenKind.Semicolon);
            elseStmt = ParseIfStatement();
        }

        return new IfStmt(condition, body, elseStmt);
    }

    private WhileStmt ParseWhileStatement()
    {
        Expression condition = ParseLogicalOr();

        // Handle ASI artifacts
        Match(TokenKind.Semicolon);

        Consume(TokenKind.BraceLeft, "Expect '{' after while condition");

        var body = ParseBlockStatement();
        return new WhileStmt(condition, body);
    }

    private ForStmt ParseForStatement()
    {
        Token identifierToken = Consume(TokenKind.Identifier, "Expect variable name");

        Consume(TokenKind.In, "Expect 'in' before range'");
        
        var range = ParseRange();
        
        var iter = new VariableDeclaration(range.Start, identifierToken.Text, TypeAnnotation.None, immutable: true);
        
        Consume(TokenKind.BraceLeft, "Expect '{' before for loop body");
        var body = ParseBlockStatement();

        return new ForStmt(iter, range, body);
    }

    private ReturnStmt ParseReturnStatement()
    {
        Token token = Previous;
        if (Match(TokenKind.Semicolon) || Current.Kind == TokenKind.BraceRight)
        {
            return new ReturnStmt(null, token);
        }

        Expression value = ParseLogicalOr();

        TerminateStatement("Expect ';' after return value", value);

        return new ReturnStmt(value, token);
    }

    private Expression ParseExpression()
    {
        return ParseAssignment();
    }

    private Expression ParseAssignment()
    {
        Expression expr = ParseLogicalOr();

        if (!Match(TokenKind.Assignment)) return expr;
        
        Token token = Peek(-2);
        Expression value = ParseAssignment();

        switch (expr)
        {
            case NameExpr variableExpr:
                return new AssignmentExpr(variableExpr, value);
            case MemberAccessExpr memberAccessExpr:
                return new AssignmentExpr(memberAccessExpr, value);
            default:
            {
                ParseError error = new(token, filename, "Invalid assignment target");
                errors.Add(error);
                break;
            }
        }

        return expr;
    }

    private Expression ParseLogicalOr()
    {
        Expression left = ParseLogicalAnd();

        while (Match(TokenKind.Or))
        {
            Token oper = Previous;
            Expression right = ParseLogicalAnd();
            left = new LogicalExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseLogicalAnd()
    {
        Expression left = ParseEquality();

        while (Match(TokenKind.And))
        {
            Token oper = Previous;
            Expression right = ParseEquality();
            left = new LogicalExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseEquality()
    {
        Expression left = ParseComparison();

        while (Match(TokenKind.Equal, TokenKind.NotEqual))
        {
            Token oper = Previous;
            Expression right = ParseComparison();
            left = new BinaryExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseComparison()
    {
        Expression left = ParseTerm();

        while (Match(TokenKind.Greater, TokenKind.GreaterEqual, TokenKind.Less, TokenKind.LessEqual))
        {
            Token oper = Previous;
            Expression right = ParseTerm();
            left = new BinaryExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseTerm()
    {
        Expression left = ParseFactor();

        while (Match(TokenKind.Plus, TokenKind.Minus))
        {
            Token oper = Previous;
            Expression right = ParseFactor();
            left = new BinaryExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseFactor()
    {
        Expression left = ParseUnary();

        while (Match(TokenKind.Slash, TokenKind.Star))
        {
            Token oper = Previous;
            Expression right = ParseUnary();
            left = new BinaryExpr(oper, left, right);
        }

        return left;
    }

    private Expression ParseUnary()
    {
        if (Match(TokenKind.Not, TokenKind.Minus))
        {
            Token oper = Previous;
            Expression right = ParseUnary();
            return new UnaryExpr(oper, right);
        }

        return ParsePostfix();
    }

    private Expression ParsePostfix()
    {
        Expression expr = ParsePrimary();

        while (true)
        {
            if (Match(TokenKind.ParenthesisLeft))
            {
                expr = CompleteCall(expr);
            }
            else if (Match(TokenKind.Dot))
            {
                Token name = Consume(TokenKind.Identifier, "Expect member name after '.'");
                expr = new MemberAccessExpr(expr, name.Text);
            }
            else if (Match(TokenKind.As))
            {
                var targetType = ParseTypeDeclaration();
                expr = new ConversionExpr(expr, targetType);
            }
            else break;
        }

        return expr;
    }

    private CallExpr CompleteCall(Expression callee)
    {
        List<Expression> args = [];
        if (Current.Kind != TokenKind.ParenthesisRight)
        {
            do
            {
                if (args.Count > ushort.MaxValue)
                {
                    errors.Add(new ParseError(Current, filename, "Argument count exceeded"));
                }

                args.Add(ParseLogicalOr());
            }
            while (Match(TokenKind.Comma));
        }
        Token closingParenthesis = Consume(TokenKind.ParenthesisRight, "Expect ')' after arguments.");
        return new CallExpr(callee, closingParenthesis, args);
    }

    private Expression ParsePrimary()
    {
        if (Match(TokenKind.None, TokenKind.LiteralTrue, TokenKind.LiteralFalse,
                TokenKind.LiteralInt, TokenKind.LiteralReal, TokenKind.LiteralString))
        {
            return ParseLiteral();
        }

        if (Match(TokenKind.Identifier))
        {
            string name = Previous.Text;
            return new NameExpr(name);
        }

        if (Match(TokenKind.ParenthesisLeft))
        {
            Expression expr = ParseLogicalOr();
            Consume(TokenKind.ParenthesisRight, "Expect ')' after expression");
            return new GroupingExpr(expr);
        }

        if (Match(TokenKind.New))
        {
            return ParseInitObject();
        }

        ParseError error = new(Current, filename, "Expect expression");
        errors.Add(error);
        throw error.Exception();
    }
    
    private InitializerExpr ParseInitObject()
    {
        var expr = ParsePostfix();
        TypeAnnotation typeName = new(expr);
        
        Consume(TokenKind.BraceLeft, "Expect '{' after type name.");

        List<Initializer> initializers = [];
        if (Current.Kind != TokenKind.BraceRight)
        {
            while (true)
            {
                Token name = Consume(TokenKind.Identifier, "Expect field name");
                Consume(TokenKind.Colon, "Expect ':' after field name.");
                var value = ParseLogicalOr();
                initializers.Add(new Initializer(name.Text, value));

                if (!Match(TokenKind.Comma)) break;

                if (Current.Kind == TokenKind.BraceRight) break;
            }
        }
        
        Consume(TokenKind.BraceRight, "Expect '}' after object initializer");
        return new InitializerExpr(typeName, initializers.ToArray());
    }

    private LiteralExpr ParseLiteral()
    {
        if (Previous.Kind == TokenKind.LiteralTrue)
        {
            return new LiteralExpr(ConstantValue.FromBool(true));
        }
        if (Previous.Kind == TokenKind.LiteralFalse)
        {
            return new LiteralExpr(ConstantValue.FromBool(false));
        }
        if (Previous.Kind == TokenKind.None)
        {
            return new LiteralExpr(ConstantValue.None());
        }

        TextSpan span = Previous.Span!.Value;

        switch (Previous.Kind)
        {
            case TokenKind.LiteralInt when
                long.TryParse(span.AsSpan(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var i):
                return new LiteralExpr(ConstantValue.FromInt(i));
            case TokenKind.LiteralReal when
                double.TryParse(span.AsSpan(), CultureInfo.InvariantCulture, out var d):
                return new LiteralExpr(ConstantValue.FromReal(d));
            case TokenKind.LiteralString:
                return new LiteralExpr(ConstantValue.FromString(span.Text));
            default:
                ParseError error = new(Previous, filename, $"{Previous.Kind} not a valid literal kind");
                errors.Add(error);
                throw error.Exception();
        }
    }
    
    private RangeExpr ParseRange()
    {
        var start = ParseTerm();

        bool isInclusive = false;
        if (Match(TokenKind.RangeInclusive))
        {
            isInclusive = true;
        }
        else if (!Match(TokenKind.Range))
        {
            var error = new ParseError(Current, filename, "Expect range operator");
            errors.Add(error);
        }
        
        var end = ParseTerm();

        Expression? step = null;
        if (Match(TokenKind.By))
        {
            step = ParseTerm();
        }
        
        return new RangeExpr(start, end, step, isInclusive);
    }

    private TypeAnnotation ParseTypeDeclaration()
    {
        Expression typeExpr = ParseTypeRef();
        return new TypeAnnotation(typeExpr);
    }

    private Expression ParseTypeRef()
    {
        var first = Consume(TokenKind.Identifier, "Expect type name");
        Expression expr = new NameExpr(first.Text);
        
        while (Match(TokenKind.Dot))
        {
            var part = Consume(TokenKind.Identifier, "Expect identifier after '.' in type name");
            expr = new MemberAccessExpr(expr, part.Text);
        }

        return expr;
    }
    
    private void TerminateStatement(string errorMessage, Expression? expr = null)
    {
        if (expr is not null && expr.Kind == ExpressionKind.Initializer) return;
        
        if (Current.Kind != TokenKind.BraceRight)
        {
            Consume(TokenKind.Semicolon, errorMessage);
        }
    }

    private void Synchronize()
    {
        if (Current.Kind == TokenKind.Semicolon)
        {
            Advance();
            return;
        }
        
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

    private Token Consume(TokenKind symbol, string message)
    {
        if (Current.Kind == symbol) return Advance();
        ParseError error = new(Current, filename, message);
        errors.Add(error);
        throw error.Exception();
    }

    private ref Token Advance()
    {
        return ref tokens[tokenIndex++];
    }

    private ref readonly Token Peek(int offset)
    {
        int peekIndex = tokenIndex + offset;
        if (peekIndex < 0 || peekIndex >= tokens.Length)
        {
            return ref Token.Empty;
        }
        return ref tokens[peekIndex];
    }

    private ref readonly Token Next => ref Peek(1);
    private ref readonly Token Current => ref Peek(0);
    private ref readonly Token Previous => ref Peek(-1);

    private bool Match(params TokenKind[] values)
    {
        if (values.All(value => Current.Kind != value)) return false;
        Advance();
        return true;
    }
}
