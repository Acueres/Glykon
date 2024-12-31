using Tython.Model;

namespace Tython.Component
{
    public class Parser(Token[] tokens, string fileName)
    {
        readonly string fileName = fileName;

        bool AtEnd => nextToken >= tokens.Length;

        readonly Token[] tokens = tokens;
        readonly List<IStatement> statements = [];
        readonly SymbolTable symbolTable = new();
        readonly List<ITythonError> errors = [];
        int nextToken;

        public (IStatement[], SymbolTable symbolTable, List<ITythonError>) Execute()
        {
            try
            {
                while (!AtEnd)
                {
                    if (Peek().Type == TokenType.EOF) break;
                    IStatement stmt = ParseStatement();
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
            if (Match(TokenType.Let))
            {
                return ParseVariableDeclaration();
            }

            if (Match(TokenType.BraceLeft))
            {
                return ParseBlockStatement();
            }

            if (Match(TokenType.If))
            {
                return ParseIfStatement();
            }

            Token token = Token.Null;
            if (Match(TokenType.Print))
            {
                token = Current();
            }

            IExpression expr = ParseExpression();

            Consume(TokenType.Semicolon, "Expect ';' after expression");

            return token.Type switch
            {
                TokenType.Print => new PrintStmt(expr),
                _ => new ExpressionStmt(expr)
            };
        }

        BlockStmt ParseBlockStatement()
        {
            int scopeIndex = symbolTable.BeginScope();

            List<IStatement> statements = [];

            while (Peek().Type != TokenType.BraceRight && !AtEnd)
            {
                statements.Add(ParseStatement());
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

        VariableStmt ParseVariableDeclaration()
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
            }

            Consume(TokenType.Semicolon, "Expect ';' after variable declaration");

            string name = token.Value.ToString();
            symbolTable.Add(name, declaredType);
            return new(initializer, name, declaredType);
        }

        TokenType ParseTypeDeclaration()
        {
            TokenType type = Peek().Type;
            Advance();
            return type;
        }

        TokenType InfereType(IExpression expression, TokenType type)
        {
            switch (expression.Type)
            {
                case ExpressionType.Literal: return ((LiteralExpr)expression).Token.Type;
                case ExpressionType.Unary:
                    {
                        UnaryExpr unaryExpr = (UnaryExpr)expression;
                        return InfereType(unaryExpr.Expr, type);
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
                        return typeLeft;
                    }
                case ExpressionType.Variable: return symbolTable.GetType(((VariableExpr)expression).Name);
                case ExpressionType.Grouping: return InfereType(((GroupingExpr)expression).Expr, type);
                default: return type;
            }
        }

        public IExpression ParseExpression()
        {
            return ParseAssignment();
        }

        IExpression ParseAssignment()
        {
            IExpression expr = ParseEquality();

            if (Match(TokenType.Assignment))
            {
                Token token = Previous();
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

        IExpression ParseEquality()
        {
            IExpression expr = ParseComparison();

            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                Token oper = Current();
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
                Token oper = Current();
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
                Token oper = Current();
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
                Token oper = Current();
                IExpression right = ParseUnary();
                expr = new BinaryExpr(oper, expr, right);
            }

            return expr;
        }

        IExpression ParseUnary()
        {
            if (Match(TokenType.Not, TokenType.Minus))
            {
                Token oper = Current();
                IExpression right = ParseUnary();
                return new UnaryExpr(oper, right);
            }

            return ParsePrimary();
        }

        IExpression ParsePrimary()
        {
            if (Match(TokenType.None, TokenType.True, TokenType.False, TokenType.Int, TokenType.Real, TokenType.String))
                return new LiteralExpr(Current());

            if (Match(TokenType.Identifier))
            {
                string name = Current().Value.ToString();
                return new VariableExpr(name);
            }

            if (Match(TokenType.ParenthesisLeft))
            {
                IExpression expr = ParseExpression();
                Consume(TokenType.ParenthesisRight, "Expect ')' after expression");
                return new GroupingExpr(expr);
            }

            ParseError error = new(Peek(), fileName, "Expect expression");
            errors.Add(error);
            throw error.Exception();
        }

        void Synchronize()
        {
            Advance();

            while (!AtEnd)
            {
                if (Current().Type == TokenType.Semicolon) return;
                switch (Peek().Type)
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
            if (Peek().Type == symbol) return Advance();
            ParseError error = new(Peek(), fileName, message);
            errors.Add(error);
            throw error.Exception();
        }

        Token Advance()
        {
            return tokens[nextToken++];
        }

        Token Peek(int offset = 0)
        {
            int nextTokenPos = nextToken + offset;
            Token token = AtEnd || nextTokenPos >= tokens.Length ? Token.Null : tokens[nextTokenPos];
            return token;
        }

        Token Current() => tokens[nextToken - 1];

        Token Previous() => tokens[nextToken - 2];

        bool Match(params TokenType[] values)
        {
            foreach (TokenType value in values)
            {
                if (Peek().Type == value)
                {
                    Advance();
                    return true;
                }
            }

            return false;
        }
    }
}
