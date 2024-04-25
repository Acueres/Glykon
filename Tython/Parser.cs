using Tython.Model;

namespace Tython
{
    public class Parser(List<Token> tokens, string fileName)
    {
        readonly List<Token> tokens = tokens;
        readonly string fileName = fileName;

        bool AtEnd => currentToken >= tokens.Count;

        int currentToken;
        bool error;

        public Expression Parse()
        {
            try
            {
                return ParseExpression();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        Expression ParseExpression()
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
                expr = new(oper, right, ExpressionType.Binary);
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
                expr = new(oper, right, ExpressionType.Binary);
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
                expr = new(oper, right, ExpressionType.Binary);
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
                expr = new(oper, right, ExpressionType.Binary);
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
            if (Match("False", "True", "None")
                || Match(TokenType.Int, TokenType.Float, TokenType.String))
                return new(Peek(-1), ExpressionType.Literal);

            if (Match("("))
            {
                Expression expr = ParseExpression();
                Consume(")", "Expect ')' after expression");
                return new(expr, ExpressionType.Grouping);
            }

            Error(Peek().Line, "Expect expression");
            throw new Exception("Expect expression");
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

        Token Consume(string lexeme, string message)
        {
            if (Peek().Lexeme == lexeme) return Advance();
            Error(Peek().Line, message);
            throw new Exception(message);
        }

        Token Advance()
        {
            return tokens[currentToken++];
        }

        Token Peek(int offset = 0)
        {
            int nextTokenPos = currentToken + offset;
            Token token = AtEnd || nextTokenPos >= tokens.Count ? Token.Null : tokens[nextTokenPos];
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

        void Error(int line, string message)
        {
            Console.WriteLine($"{message} ({fileName}, line {line})");
            error = true;
        }
    }
}
