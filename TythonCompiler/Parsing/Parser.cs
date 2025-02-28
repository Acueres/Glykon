using TythonCompiler.Syntax;
using TythonCompiler.Tokenization;
using TythonCompiler.SemanticAnalysis;
using TythonCompiler.Diagnostics.Exceptions;
using TythonCompiler.Diagnostics.Errors;
using TythonCompiler.Syntax.Expressions;
using TythonCompiler.Syntax.Statements;

namespace TythonCompiler.Parsing
{
    public class Parser(Token[] tokens, string fileName)
    {
        readonly string fileName = fileName;

        bool AtEnd => tokenIndex >= tokens.Length;

        readonly Token[] tokens = tokens;
        readonly List<IStatement> statements = [];
        readonly SymbolTable symbolTable = new();
        readonly List<ITythonError> errors = [];
        int tokenIndex;

        public (IStatement[], SymbolTable symbolTable, List<ITythonError>) Execute()
        {
            try
            {
                while (!AtEnd)
                {
                    if (Current.Type == TokenType.EOF) break;
                    IStatement stmt = ParseStatement();
                    if (stmt is null) continue;
                    statements.Add(stmt);
                }

                return (statements.ToArray(), symbolTable, errors);
            }
            catch (ParseException)
            {
                Synchronize();
            }

            return ([], symbolTable, errors);
        }

        IStatement ParseStatement()
        {
            if (Match(TokenType.Const))
            {
                ParseConstantDeclaration();
                return null;
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
                return ParseBlockStatement();
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
                Consume(TokenType.Semicolon, "Expect ';' after break or continue");
                return jumpStmt;

            }

            if (Match(TokenType.Def))
            {
                return ParseFunctionDeclaration();
            }

            IExpression expr = ParseExpression();
            Consume(TokenType.Semicolon, "Expect ';' after expression");

            return new ExpressionStmt(expr);
        }

        BlockStmt ParseBlockStatement()
        {
            int scopeIndex = symbolTable.BeginScope();

            List<IStatement> statements = [];

            while (Current.Type != TokenType.BraceRight && !AtEnd)
            {
                IStatement stmt = ParseStatement();
                if (stmt is null) continue;
                statements.Add(stmt);
            }

            Consume(TokenType.BraceRight, "Expect '}' after block");

            symbolTable.ExitScope();

            return new BlockStmt(statements, scopeIndex);
        }

        IfStmt ParseIfStatement()
        {
            IExpression condition = ParseExpression();

            TokenType conditionType = InfereType(condition, TokenType.Bool);
            if (!(conditionType == TokenType.Bool
            || conditionType == TokenType.True
            || conditionType == TokenType.False))
            {
                errors.Add(new TypeError($"Type mismatch: expected bool, got {conditionType}", fileName));
            }

            Consume(TokenType.BraceLeft, "Expect '{' after if condition");

            IStatement stmt = ParseBlockStatement();

            IStatement? elseStmt = null;
            if (Match(TokenType.Else))
            {
                elseStmt = ParseStatement();
            }
            else if (Match(TokenType.Elif))
            {
                elseStmt = ParseIfStatement();
            }

            return new IfStmt(condition, stmt, elseStmt);
        }

        WhileStmt ParseWhileStatement()
        {
            IExpression condition = ParseExpression();

            TokenType conditionType = InfereType(condition, TokenType.Bool);
            if (!(conditionType == TokenType.Bool
            || conditionType == TokenType.True
            || conditionType == TokenType.False))
            {
                errors.Add(new TypeError($"Type mismatch: expected bool, got {conditionType}", fileName));
            }

            Consume(TokenType.BraceLeft, "Expect '{' after if condition");

            IStatement body = ParseBlockStatement();

            return new WhileStmt(condition, body);
        }

        FunctionStmt ParseFunctionDeclaration()
        {
            Token functionName = Consume(TokenType.Identifier, "Expect function name");
            Consume(TokenType.ParenthesisLeft, "Expect '(' after function name");
            List<Parameter> parameters = [];

            int scopeIndex = symbolTable.BeginScope();
            if (Current.Type != TokenType.ParenthesisRight)
            {
                do
                {
                    if (parameters.Count > ushort.MaxValue)
                    {
                        errors.Add(new ParseError(Current, fileName, "Argument count exceeded"));
                    }

                    Token name = Consume(TokenType.Identifier, "Expect parameter name");
                    Consume(TokenType.Colon, "Expect colon before type declaration");
                    Token type = Advance();

                    Parameter parameter = new(name.Value as string, type.Type);
                    parameters.Add(parameter);

                    symbolTable.RegisterParameter(parameter.Name, parameter.Type);
                }
                while (Match(TokenType.Comma));
            }

            Consume(TokenType.ParenthesisRight, "Expect ')' after parameters");

            TokenType returnType = TokenType.None;
            if (Match(TokenType.Arrow))
            {
                returnType = ParseTypeDeclaration();
            }

            Consume(TokenType.BraceLeft, "Expect '{' before function body");
            List<IStatement> body = [];

            while (Current.Type != TokenType.BraceRight && !AtEnd)
            {
                IStatement stmt = ParseStatement();
                if (stmt is null) continue;
                body.Add(stmt);
            }

            Consume(TokenType.BraceRight, "Expect '}' after function body");

            symbolTable.ExitScope();

            var signature = symbolTable.RegisterFunction((string)functionName.Value, returnType, [.. parameters.Select(p => p.Type)]);

            return new FunctionStmt((string)functionName.Value, signature, scopeIndex, parameters, returnType, body);
        }

        ReturnStmt ParseReturnStatement()
        {
            if (Match(TokenType.Semicolon))
            {
                return new ReturnStmt(null);
            }

            IExpression value = ParseExpression();

            Consume(TokenType.Semicolon, "Expect ';' after return value");
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
            if (initializer == null && declaredType == TokenType.None)
            {
                ParseError error = new(token, fileName, "Expect type declaration");
                errors.Add(error);
                throw error.Exception();
            }
            else
            {
                TokenType inferredType = InfereType(initializer, declaredType);
                if (declaredType == TokenType.None)
                {
                    declaredType = inferredType;
                }
                else if (declaredType != inferredType)
                {
                    ParseError error = new(token, fileName, "Type mismatch");
                    errors.Add(error);
                    throw error.Exception();
                }


                Consume(TokenType.Semicolon, "Expect ';' after variable declaration");

                string name = token.Value.ToString();
                symbolTable.RegisterVariable(name, declaredType);
                return new(initializer, name, declaredType);
            }
        }

        void ParseConstantDeclaration()
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

            TokenType inferredType = InfereType(literal, declaredType);
            if (declaredType != inferredType)
            {
                ParseError error = new(token, fileName, "Type mismatch");
                errors.Add(error);
                throw error.Exception();
            }

            Consume(TokenType.Semicolon, "Expect ';' after constant declaration");

            string name = (string)token.Value;
            symbolTable.RegisterConstant(name, literal.Token.Value, declaredType);
        }

        TokenType ParseTypeDeclaration()
        {
            return Advance().Type;
        }

        TokenType InfereType(IExpression expression, TokenType type)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal: return ((LiteralExpr)expression).Token.Type;
                case ExpressionType.Unary:
                    {
                        UnaryExpr unaryExpr = (UnaryExpr)expression;
                        return InfereType(unaryExpr.Expression, type);
                    }
                case ExpressionType.Binary:
                    {
                        BinaryExpr binaryExpr = (BinaryExpr)expression;
                        TokenType typeLeft = InfereType(binaryExpr.Left, type);
                        TokenType typeRight = InfereType(binaryExpr.Right, type);
                        if (typeLeft != typeRight)
                        {
                            ParseError error = new(binaryExpr.Operator, fileName,
                                $"Operator {binaryExpr.Operator.Type} cannot be applied between types '{typeLeft}' and '{typeRight}'");
                            errors.Add(error);
                            throw error.Exception();
                        }

                        if (binaryExpr.Operator.Type == TokenType.Equal
                            || binaryExpr.Operator.Type == TokenType.NotEqual
                            || binaryExpr.Operator.Type == TokenType.Greater
                            || binaryExpr.Operator.Type == TokenType.Less
                            || binaryExpr.Operator.Type == TokenType.GreaterEqual
                            || binaryExpr.Operator.Type == TokenType.LessEqual)
                        {
                            return TokenType.Bool;
                        }

                        return typeLeft;
                    }
                case ExpressionType.Variable: return symbolTable.GetType(((VariableExpr)expression).Name);
                case ExpressionType.Grouping: return InfereType(((GroupingExpr)expression).Expression, type);
                default: return type;
            }
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
                Token token = Previous2;
                IExpression value = ParseAssignment();

                if (expr.Type == ExpressionType.Variable)
                {
                    string name = ((VariableExpr)expr).Name;

                    TokenType variableType = symbolTable.GetType(name);
                    TokenType valueType = InfereType(value, variableType);

                    if (variableType != valueType)
                    {
                        errors.Add(new ParseError(token, fileName, $"Type mismatch; can't assign {valueType} to {variableType}"));
                        return expr;
                    }

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

                TokenType leftType = InfereType(expr, TokenType.Bool);
                TokenType rightType = InfereType(right, TokenType.Bool);

                bool isLeftBool = leftType == TokenType.True || leftType == TokenType.False || leftType == TokenType.Bool;
                bool isRightBool = rightType == TokenType.True || rightType == TokenType.False || rightType == TokenType.Bool;

                if (!(isLeftBool && isRightBool))
                {
                    errors.Add(new ParseError(oper, fileName, $"Type mismatch; operator {oper.Type} cannot be applied between types {leftType} and {rightType}"));
                    return expr;
                }

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

                TokenType leftType = InfereType(expr, TokenType.Bool);
                TokenType rightType = InfereType(right, TokenType.Bool);

                bool isLeftBool = leftType == TokenType.True || leftType == TokenType.False || leftType == TokenType.Bool;
                bool isRightBool = rightType == TokenType.True || rightType == TokenType.False || rightType == TokenType.Bool;

                if (!(isLeftBool && isRightBool))
                {
                    errors.Add(new ParseError(oper, fileName, $"Type mismatch; operator {oper.Type} cannot be applied between types {leftType} and {rightType}"));
                    return expr;
                }

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
            if (Current.Type != TokenType.ParenthesisRight)
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
            if (Match(TokenType.None, TokenType.True, TokenType.False, TokenType.Int, TokenType.Real, TokenType.String))
                return new LiteralExpr(Previous);

            if (Match(TokenType.Identifier))
            {
                string name = Previous.Value.ToString();
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

        void Synchronize()
        {
            Advance();

            while (!AtEnd)
            {
                if (Current.Type == TokenType.Semicolon) return;
                switch (Current.Type)
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
            if (Current.Type == symbol) return Advance();
            ParseError error = new(Current, fileName, message);
            errors.Add(error);
            throw error.Exception();
        }

        Token Advance()
        {
            return tokens[tokenIndex++];
        }

        Token GetToken(int offset)
        {
            int nextTokenPos = tokenIndex + offset;
            Token token = AtEnd || nextTokenPos >= tokens.Length ? Token.Null : tokens[nextTokenPos];
            return token;
        }

        Token Current => GetToken(0);

        Token Previous => GetToken(-1);

        Token Previous2 => GetToken(-2);

        bool Match(params TokenType[] values)
        {
            foreach (TokenType value in values)
            {
                if (Current.Type == value)
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }
    }
}
