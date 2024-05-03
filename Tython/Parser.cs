using Tython.Model;

namespace Tython
{
    public class Parser(Token[] tokens, string fileName)
    {
        readonly string fileName = fileName;

        bool AtEnd => currentToken >= tokens.Length;

        readonly Token[] tokens = tokens;
        readonly List<Statement> statements = [];
        readonly List<ITythonError> errors = [];
        int currentToken;

        public (List<Statement>, List<ITythonError>) Parse()
        {
            try
            {
                while (!AtEnd)
                {
                    Statement stmt = ParseStatement();
                    statements.Add(stmt);
                }
                return (statements, errors);
            }
            catch (ParseException)
            {
                Synchronize();
                return ([], errors);
            }
        }

        public Statement ParseStatement()
        {
            Expression expr = ParseExpression();
            Consume(";", "Expect ';' after expression");
            return new(tokens[currentToken], expr);
        }

        public Expression ParseExpression()
        {
            return ParseEquality();
        }

        Expression ParseEquality()
        {
            Expression expr = ParseComparison();

            while (Match("!=", "=="))
            {
                Token oper = Peek(-1);
                Expression right = ParseComparison();
                expr = new(oper, expr, right, ExpressionType.Binary);
            }

            return expr;
        }

        Expression ParseComparison()
        {
            Expression expr = ParseTerm();

            while (Match(">", ">=", "<", "<="))
            {
                Token oper = Peek(-1);
                Expression right = ParseTerm();
                expr = new(oper, expr, right, ExpressionType.Binary);
            }

            return expr;
        }

        Expression ParseTerm()
        {
            Expression expr = ParseFactor();

            while (Match("-", "+"))
            {
                Token oper = Peek(-1);
                Expression right = ParseFactor();
                expr = new(oper, expr, right, ExpressionType.Binary);
            }

            return expr;
        }

        Expression ParseFactor()
        {
            Expression expr = ParseUnary();

            while (Match("/", "*"))
            {
                Token oper = Peek(-1);
                Expression right = ParseUnary();
                expr = new(oper, expr, right, ExpressionType.Binary);
            }

            return expr;
        }

        Expression ParseUnary()
        {
            if (Match("not", "-"))
            {
                Token oper = Peek(-1);
                Expression right = ParseUnary();
                return new(oper, right, ExpressionType.Unary);
            }

            return ParsePrimary();
        }

        Expression ParsePrimary()
        {
            if (Match("false", "true", "none")
                || Match(TokenType.Int, TokenType.Real, TokenType.String))
                return new(Peek(-1), ExpressionType.Literal);

            if (Match("("))
            {
                Expression expr = ParseExpression();
                Consume(")", "Expect ')' after expression");
                return new(expr, ExpressionType.Grouping);
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
                if (Peek(-1).Lexeme == ";") return;
                switch (Peek().Lexeme)
                {
                    case "class":
                    case "struct":
                    case "interface":
                    case "enum":
                    case "def":
                    case "for":
                    case "if":
                    case "while":
                    case "return":
                        return;
                }

                Advance();
            }
        }

        Token Consume(string symbol, string message)
        {
            if (Peek().Lexeme == symbol) return Advance();
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

        bool Match(params string[] values)
        {
            foreach (string value in values)
            {
                if (Peek().Lexeme == value)
                {
                    Advance();
                    return true;
                }
            }

            return false;
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
