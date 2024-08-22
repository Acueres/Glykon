using System.Globalization;
using Tython.Enum;
using Tython.Model;

namespace Tython
{
    public class Lexer(string source, string fileName)
    {
        readonly string source = source;
        readonly string fileName = fileName;

        bool AtEnd => currentChar >= source.Length;

        readonly List<Token> tokens = [];
        readonly List<ITythonError> errors = [];
        int line = 0;
        int currentChar = 0;

        public (Token[] tokens, List<ITythonError> errors) Execute()
        {
            while (!AtEnd)
            {
                Token token = GetNextToken();
                if (!token.IsNull)
                    tokens.Add(token);
            }

            tokens.Add(new(TokenType.EOF, line));
            return (tokens.ToArray(), errors);
        }

        Token GetNextToken()
        {
            char character = Advance();

            switch (character)
            {
                //symbols
                case '{':
                    return new(TokenType.BraceLeft, line);
                case '}':
                    return new(TokenType.BraceRight, line);
                case '(':
                    return new(TokenType.ParenthesisLeft, line);
                case ')':
                    return new(TokenType.ParenthesisRight, line);
                case '[':
                    return new(TokenType.BracketLeft, line);
                case ']':
                    return new(TokenType.BracketRight, line);
                case ',':
                    return new(TokenType.Comma, line);
                case ':':
                    return new(TokenType.Colon, line);

                //long symbols
                case '.':
                    {
                        if (char.IsAsciiDigit(Peek()))
                        {
                            return ScanNumber(true);
                        }

                        return new(TokenType.Dot, line);
                    }
                case '+':
                    return new(TokenType.Plus, line);
                case '-':
                    return new(TokenType.Minus, line);
                case '<':
                    return new(Match('=') ? TokenType.LessEqual : TokenType.Less, line);
                case '>':
                    return new(Match('=') ? TokenType.GreaterEqual : TokenType.Greater, line);
                case '*':
                    return new(Match('*') ? TokenType.StarDouble : TokenType.Star, line);
                case '/':
                    return new(Match('/') ? TokenType.SlashDouble : TokenType.Slash, line);
                case '=':
                    return new(Match('=') ? TokenType.Equal : TokenType.Assignment, line);
                case '!': //! is not valid by itself
                    if (Match('='))
                    {
                        return new(TokenType.NotEqual, line);
                    }
                    else
                    {
                        errors.Add(new SyntaxError(line, fileName, "Syntax Error: invalid syntax"));
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

                    errors.Add(new SyntaxError(line, fileName, $"Invalid character '{character}' in token"));
                    return Token.Null;
            }

            return Token.Null;
        }

        Token ScanIdentifier()
        {
            int identifierStart = currentChar - 1;

            while (char.IsLetterOrDigit(Peek())) Advance();

            string identifier = source[identifierStart..currentChar];

            if (keywords.Contains(identifier))
            {
                return new(keywordToType[identifier], line);
            }

            return new(identifier, line, TokenType.Identifier);
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

            string number = source[numberStart..currentChar];
            object value;
            if (isFloat)
            {
                value = double.Parse(number, CultureInfo.InvariantCulture);
            }
            else
            {
                value = long.Parse(number);
            }

            return new(value, line, isFloat ? TokenType.Real : TokenType.Int);
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
                        errors.Add(new SyntaxError(line, fileName, "SyntaxError: unterminated string literal"));
                        return Token.Null;
                    }

                    line++;
                }

                Advance();
            }

            if (AtEnd)
            {
                errors.Add(new SyntaxError(line, fileName, "SyntaxError: unterminated string literal"));
                return Token.Null;
            }

            int stringEndOffset = multiline ? 3 : 1;
            Token result = new(source[stringStart..(currentChar - stringEndOffset)], currentLine, TokenType.String);

            return result;
        }

        Token ScanTerminator()
        {
            Token last = tokens.LastOrDefault();
            if (last.Type != TokenType.Semicolon && last.Type != TokenType.Null)
            {
                return new(TokenType.Semicolon, line);
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

        readonly static HashSet<string> keywords;
        readonly static Dictionary<string, TokenType> keywordToType;

        static Lexer()
        {
            keywords =
            [
                "class", "struct", "interface", "enum", "def", "let", "print",
                "int", "real", "str", "true", "false", "none",
                "and", "not", "or",
                "if", "else", "elif", "for", "while", "return", "break", "continue",
            ];

            keywordToType = new()
            {
                { "class", TokenType.Class },
                { "struct", TokenType.Struct },
                { "interface", TokenType.Interface },
                { "enum", TokenType.Enum },
                { "def", TokenType.Def },
                { "let", TokenType.Let },
                { "print", TokenType.Print },
                { "int", TokenType.Int },
                { "real", TokenType.Real },
                { "str", TokenType.String },
                { "bool",  TokenType.Bool },
                { "true",  TokenType.True },
                { "false",  TokenType.False },
                { "none", TokenType.None },
                { "and", TokenType.And },
                { "not", TokenType.Not },
                { "or", TokenType.Or },
                { "if", TokenType.If },
                { "else", TokenType.Else },
                { "elif", TokenType.Elif },
                { "for", TokenType.For },
                { "while", TokenType.While },
                { "return", TokenType.Return },
                { "break", TokenType.Break },
                { "continue", TokenType.Continue }
            };
        }
    }
}
