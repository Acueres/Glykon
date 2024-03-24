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
            //symbols
            if (singleSymbols.Contains(character))
            {
                return new(character.ToString(), line, TokenType.Symbol);
            }

            switch (character)
            {
                //long symbols
                case '.':
                    return new(character.ToString(), line, TokenType.Symbol);
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

        Token ScanNumber()
        {
            int numberStart = currentChar - 1;

            while (char.IsAsciiDigit(Peek())) Advance();

            return new(source[numberStart..currentChar], line, TokenType.Int);
        }

        Token ScanString(char openingQuote)
        {
            int stringStart = currentChar;

            while (Peek() != openingQuote && !AtEnd)
            {
                if (Peek() == '\n')
                {
                    Error(line, "SyntaxError: unterminated string literal");
                    return Token.Null;
                }

                Advance();
            }

            if (AtEnd)
            {
                Error(line, "SyntaxError: unterminated string literal");
                return Token.Null;
            }

            Token result = new(source[stringStart..currentChar], line, TokenType.String);

            Advance(); //closing quote

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

        char Peek(int n = 0)
        {
            int nextCharPos = currentChar + n;
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
        readonly static HashSet<string> symbols;
        readonly static HashSet<char> singleSymbols;

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

            symbols =
            [
                "{", "}", "(", ")", "[", "]", "=", ":", ";",
                ".", ",", "+", "-", "*", "/", "**", "//",
                "<", ">", "<=", ">=", "==", "!="
            ];

            singleSymbols = ['{', '}', '(', ')', '[', ']', ',', ';', ':'];
        }
    }
}
