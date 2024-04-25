using Tython.Model;

namespace Tython
{
    public class Lexer(string source, string fileName)
    {
        readonly string source = source;
        readonly string fileName = fileName;

        bool AtEnd => currentChar >= source.Length;
        bool error = false;

        readonly List<Token> tokens = [];
        int line = 0;
        int currentChar = 0;

        public List<Token> ScanSource()
        {
            while (!AtEnd)
            {
                Token token = GetNextToken();
                if (!token.IsNull)
                    tokens.Add(token);
            }

            return tokens;
        }

        Token GetNextToken()
        {
            char character = Advance();

            switch (character)
            {
                //symbols
                case '{':
                case '}':
                case '(':
                case ')':
                case '[':
                case ']':
                case ',':
                case ':':
                    return new(character.ToString(), line, TokenType.Symbol);

                //long symbols
                case '.':
                    {
                        if (char.IsAsciiDigit(Peek()))
                        {
                            return ScanNumber(true);
                        }

                        return new(character.ToString(), line, TokenType.Symbol);
                    }
                case '+':
                    return new(character.ToString(), line, TokenType.Symbol);
                case '-':
                    return new(character.ToString(), line, TokenType.Symbol);
                case '<':
                    return new(Match('=') ? "<=" : "<", line, TokenType.Symbol);
                case '>':
                    return new(Match('=') ? ">=" : ">", line, TokenType.Symbol);
                case '*':
                    return new(Match('*') ? "**" : "*", line, TokenType.Symbol);
                case '/':
                    return new(Match('/') ? "//" : "/", line, TokenType.Symbol);
                case '=':
                    return new(Match('=') ? "==" : "=", line, TokenType.Symbol);
                case '!': //! is not valid by itself
                    if (Match('='))
                    {
                        return new("!=", line, TokenType.Symbol);
                    }
                    else
                    {
                        Error(line, "Syntax Error: invalid syntax");
                    }
                    break;

                //strings
                case '\'':
                case '"':
                    return ScanString(character);

                //whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;

                //comments
                case '#':
                    while (Peek() != '\n') Advance();
                    break;

                //statement terminator
                case '\n':
                    line++;
                    return ScanTerminator();
                case ';':
                    return ScanTerminator();

                default:
                    if (char.IsAsciiDigit(character))
                    {
                        return ScanNumber();
                    }

                    if (char.IsLetter(character))
                    {
                        return ScanIdentifier();
                    }

                    Error(line, $"Invalid character '{character}' in token");
                    return Token.Null;
            }

            return Token.Null;
        }

        Token ScanIdentifier()
        {
            int identifierStart = currentChar - 1;

            while (char.IsLetterOrDigit(Peek())) Advance();

            string identifier = source[identifierStart..currentChar];

            return new(identifier, line, keywords.Contains(identifier) ? TokenType.Keyword : TokenType.Identifier);
        }

        Token ScanNumber(bool isFloat = false)
        {
            int numberStart = currentChar - 1;

            while (char.IsAsciiDigit(Peek())) Advance();

            if (Peek() == '.' && char.IsAsciiDigit(Peek(1)))
            {
                isFloat = true;

                Advance();

                while (char.IsAsciiDigit(Peek())) Advance();
            }

            return new(source[numberStart..currentChar], line, isFloat ? TokenType.Float : TokenType.Int);
        }

        Token ScanString(char openingQuote)
        {
            bool multiline = Match(openingQuote, 2);
            int currentLine = line;
            int stringStart = currentChar;

            while (!AtEnd && !(multiline ? Match(openingQuote, 3) : Match(openingQuote)))
            {
                if (Peek() == '\n')
                {
                    if (!multiline)
                    {
                        Error(line, "SyntaxError: unterminated string literal");
                        return Token.Null;
                    }

                    line++;
                }

                Advance();
            }

            if (AtEnd)
            {
                Error(line, "SyntaxError: unterminated string literal");
                return Token.Null;
            }

            int stringEndOffset = multiline ? 3 : 1;
            Token result = new(source[stringStart..(currentChar - stringEndOffset)], currentLine, TokenType.String);

            return result;
        }

        Token ScanTerminator()
        {
            Token last = tokens.LastOrDefault();
            if (last.Lexeme != ";" && last.Lexeme != null)
            {
                return new(";", line, TokenType.Symbol);

            }

            return Token.Null;
        }

        bool Match(char token)
        {
            if (AtEnd || Peek() != token) return false;
            currentChar++;
            return true;
        }

        bool Match(char token, int offset)
        {
            for (int i = 0; i < offset; i++)
            {
                if (AtEnd || Peek(i) != token) return false;
            }

            currentChar += offset;

            return true;
        }

        char Peek(int offset = 0)
        {
            int nextCharPos = currentChar + offset;
            char c = AtEnd || nextCharPos >= source.Length ? '\0' : source[nextCharPos];
            return c;
        }

        char Advance()
        {
            return source[currentChar++];
        }

        void Error(int line, string message)
        {
            Console.WriteLine($"{message} ({fileName}, line {line})");
            error = true;
        }

        readonly static HashSet<string> keywords;

        static Lexer()
        {
            keywords =
            [
                "class", "struct", "interface", "enum", "def",
                "int", "float", "bool", "str",
                "and", "not", "or",
                "if", "else", "elif", "for", "while", "return",
                "True", "False", "None"
            ];
        }
    }
}
