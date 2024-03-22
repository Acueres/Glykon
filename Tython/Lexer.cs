using System.Text;

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
                char token = NextToken();
                ScanToken(token);
            }

            return tokens;
        }

        void ScanToken(char token)
        {
            switch (token)
            {
                //symbols
                case '{':
                    AddToken("{", line, TokenType.Symbol);
                    break;
                case '}':
                    AddToken("}", line, TokenType.Symbol);
                    break;
                case '(':
                    AddToken("(", line, TokenType.Symbol);
                    break;
                case ')':
                    AddToken(")", line, TokenType.Symbol);
                    break;
                case '[':
                    AddToken("[", line, TokenType.Symbol);
                    break;
                case ']':
                    AddToken("]", line, TokenType.Symbol);
                    break;
                case ':':
                    AddToken(":", line, TokenType.Symbol);
                    break;
                case '.':
                    AddToken(".", line, TokenType.Symbol);
                    break;
                case ',':
                    AddToken(",", line, TokenType.Symbol);
                    break;
                case '+':
                    AddToken("+", line, TokenType.Symbol);
                    break;
                case '-':
                    AddToken("-", line, TokenType.Symbol);
                    break;

                //long symbols
                case '<':
                    AddToken(MatchToken('=') ? "<=" : "<", line, TokenType.Symbol);
                    break;
                case '>':
                    AddToken(MatchToken('=') ? ">=" : ">", line, TokenType.Symbol);
                    break;
                case '*':
                    AddToken(MatchToken('*') ? "**" : "*", line, TokenType.Symbol);
                    break;
                case '/':
                    AddToken(MatchToken('/') ? "//" : "/", line, TokenType.Symbol);
                    break;
                case '=':
                    AddToken(MatchToken('=') ? "==" : "=", line, TokenType.Symbol);
                    break;
                case '!': //! is not valid by itself
                    if (MatchToken('='))
                    {
                        AddToken("!=", line, TokenType.Symbol);
                    }
                    else
                    {
                        Error(line, "Syntax Error: invalid syntax");
                    }
                    break;

                //strings
                case '\'':
                case '"':
                    ScanString(token);
                    break;

                //whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;

                //comments
                case '#':
                    while (Lookup() != '\n') NextToken();
                    break;

                //statement terminator
                case '\n':
                    {
                        Token last = tokens.LastOrDefault();
                        if (last.Lexeme != ";" && last.Lexeme != null)
                            AddToken(";", line, TokenType.Symbol);
                        line++;
                        break;
                    }
                case ';':
                    {
                        Token last = tokens.LastOrDefault();
                        if (last.Lexeme != ";" && last.Lexeme != null)
                            AddToken(";", line, TokenType.Symbol);
                        break;
                    }

                default:
                    if (char.IsAsciiDigit(token))
                    {
                        ScanNumber();
                        break;
                    }

                    ScanIdentifier();
                    break;
            }
        }

        void ScanIdentifier()
        {
            int identifierStart = currentChar - 1;

            while (Lookup() != ' ' && Lookup() != '\n') NextToken();

            AddToken(source[identifierStart..(currentChar - 1)], line, TokenType.Identifier);
        }

        void ScanNumber()
        {
            int numberStart = currentChar - 1;

            while (char.IsAsciiDigit(Lookup())) NextToken();

            AddToken(source[numberStart..currentChar], line, TokenType.Int);
        }

        void ScanString(char openingQuote)
        {
            int stringStart = currentChar;

            while (Lookup() != openingQuote && !AtEnd)
            {
                if (Lookup() == '\n')
                {
                    Error(line, "SyntaxError: unterminated string literal");
                    return;
                }

                NextToken();
            }

            if (AtEnd)
            {
                Error(line, "SyntaxError: unterminated string literal");
                return;
            }

            string result = source[stringStart..currentChar];
            AddToken(result, line, TokenType.String);

            NextToken(); //closing quote
        }

        void AddToken(string lexeme, int line, TokenType type)
        {
            tokens.Add(new(lexeme, line, type));
        }

        bool MatchToken(char token)
        {
            if (AtEnd || Lookup() != token) return false;
            currentChar++;
            return true;
        }

        char Lookup(int n = 0)
        {
            int nextCharPos = currentChar + n;
            char c = AtEnd || nextCharPos >= sourceLength ? '\0' : source[nextCharPos];
            return c;
        }

        char NextToken()
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
        }
    }
}
