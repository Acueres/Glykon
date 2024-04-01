using Tython.Model;

namespace Tython
{
    public class Lexer(string source, string fileName)
    {
        readonly string source = source;
        readonly string fileName = fileName;
        readonly int sourceLength = source.Length;

        bool AtEnd => currentChar >= sourceLength;
        bool error = false;

        readonly List<Token> tokens = [];
        int line = 0;
        int currentChar = 0;

        public List<Token> ScanSource()
        {
            while (!AtEnd)
            {
                char character = Advance();
                Token token = ScanToken(character);
                if (!token.IsNull)
                    tokens.Add(token);
            }

            return tokens;
        }

        Token ScanToken(char character)
        {
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
                    return new(MatchToken('=') ? "<=" : "<", line, TokenType.Symbol);
                case '>':
                    return new(MatchToken('=') ? ">=" : ">", line, TokenType.Symbol);
                case '*':
                    return new(MatchToken('*') ? "**" : "*", line, TokenType.Symbol);
                case '/':
                    return new(MatchToken('/') ? "//" : "/", line, TokenType.Symbol);
                case '=':
                    return new(MatchToken('=') ? "==" : "=", line, TokenType.Symbol);
                case '!': //! is not valid by itself
                    if (MatchToken('='))
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

                    return ScanIdentifier();
            }

            return Token.Null;
        }

        Token ScanIdentifier()
        {
            int identifierStart = currentChar - 1;

            while (Peek() != ' ' && Peek() != '\n') Advance();

            return new(source[identifierStart..(currentChar - 1)], line, TokenType.Identifier);
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
            bool multiline = MatchTokens(openingQuote, 2);
            int currentLine = line;
            int stringStart = currentChar;

            while (!AtEnd && !(multiline ? MatchTokens(openingQuote, 3) : MatchToken(openingQuote)))
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

        bool MatchToken(char token)
        {
            if (AtEnd || Peek() != token) return false;
            currentChar++;
            return true;
        }

        bool MatchTokens(char token, int offset)
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
            char c = AtEnd || nextCharPos >= sourceLength ? '\0' : source[nextCharPos];
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
        readonly static HashSet<string> statements;

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

            statements = ["if", "while", "for", "return"];
        }
    }
}
