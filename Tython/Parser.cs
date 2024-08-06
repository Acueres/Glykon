using Tython.Enum;
using Tython.Model;

namespace Tython
{
    public class Parser(Token[] tokens, string fileName)
    {
        readonly string fileName = fileName;

        bool AtEnd => currentToken >= tokens.Length;

        readonly Token[] tokens = tokens;
        readonly List<Statement> statements = [];
        readonly SymbolTable symbolTable = new();
        readonly List<ITythonError> errors = [];
        int currentToken;

        public (Statement[], SymbolTable symbolTable, List<ITythonError>) Execute()
        {
            try
            {
                while (!AtEnd)
                {
                    if (Peek().Type == TokenType.EOF) break;
                    Statement stmt = ParseDeclaration();
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

        public Statement ParseDeclaration()
        {
            if (Match(TokenType.Let))
            {
                return ParseVariableDeclaration();
            }

            return ParseStatement();
        }

        Statement ParseVariableDeclaration()
        {
            Token token = Consume(TokenType.Identifier, "Expect variable name");
            IExpression? initializer = null;
            if (Match(TokenType.Assignment))
            {
                initializer = ParseExpression();
            }

            Consume(TokenType.Semicolon, "Expect ';' after variable declaration");

            symbolTable.Add(token.Value.ToString());
            return new(token, initializer, StatementType.Variable);

        }

        Statement ParseStatement()
        {
            Token token = Advance();
            IExpression expr = ParseExpression();

            Consume(TokenType.Semicolon, "Expect ';' after expression");

            var stmtType = token.Type switch
            {
                TokenType.Print => StatementType.Print,
                _ => StatementType.Expression,
            };
            return new(token, expr, stmtType);
        }

        public IExpression ParseExpression()
        {
            return ParseEquality();
        }

        IExpression ParseEquality()
        {
            IExpression expr = ParseComparison();

            while (Match(TokenType.Equal, TokenType.NotEqual))
            {
                Token oper = Peek(-1);
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
                Token oper = Peek(-1);
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
                Token oper = Peek(-1);
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
                Token oper = Peek(-1);
                IExpression right = ParseUnary();
                expr = new BinaryExpr(oper, expr, right);
            }

            return expr;
        }

        IExpression ParseUnary()
        {
            if (Match(TokenType.Not, TokenType.Minus))
            {
                Token oper = Peek(-1);
                IExpression right = ParseUnary();
                return new UnaryExpr(oper, right);
            }

            return ParsePrimary();
        }

        IExpression ParsePrimary()
        {
            if (Match(TokenType.None, TokenType.True, TokenType.False, TokenType.Int, TokenType.Real, TokenType.String))
                return new LiteralExpr(Peek(-1));

            if (Match(TokenType.Identifier))
            {
                return new VariableExpr(Peek(-1));
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
                if (Peek(-1).Type == TokenType.Semicolon) return;
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
            return tokens[currentToken++];
        }

        Token Peek(int offset = 0)
        {
            int nextTokenPos = currentToken + offset;
            Token token = AtEnd || nextTokenPos >= tokens.Length ? Token.Null : tokens[nextTokenPos];
            return token;
        }

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
