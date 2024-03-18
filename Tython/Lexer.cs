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

        readonly List<Token> tokens = [];
        int line = 0;
        int currentChar = 0;

        public List<Token> ScanSource()
        {
            StringBuilder lexeme = new();
            while (!AtEnd)
            {
                char token = NextToken();
                bool added = ScanToken(token);
                if (!added)
                {
                    lexeme.Append(token);
                }

                if ((token == ' ' || token == '\n') && lexeme.Length > 0)
                {
                    AddToken(lexeme.ToString(), line, TokenType.Identifier);
                    lexeme.Clear();
                }
            }

            return tokens;
        }

        bool ScanToken(char token)
        {
            switch (token)
            {
                //symbols
                case '{':
                    AddToken("{", line, TokenType.Symbol);
                    return true;
                case '}':
                    AddToken("}", line, TokenType.Symbol);
                    return true;
                case '(':
                    AddToken("(", line, TokenType.Symbol);
                    return true;
                case ')':
                    AddToken(")", line, TokenType.Symbol);
                    return true;
                case '[':
                    AddToken("[", line, TokenType.Symbol);
                    return true;
                case ']':
                    AddToken("]", line, TokenType.Symbol);
                    return true;
                case ':':
                    AddToken(":", line, TokenType.Symbol);
                    return true;
                case '.':
                    AddToken(".", line, TokenType.Symbol);
                    return true;
                case ',':
                    AddToken(",", line, TokenType.Symbol);
                    return true;
                case '+':
                    AddToken("+", line, TokenType.Symbol);
                    return true;
                case '-':
                    AddToken("-", line, TokenType.Symbol);
                    return true;

                //long symbols
                case '<':
                    if (Lookup() == '=')
                    {
                        NextToken();
                        AddToken("<=", line, TokenType.Symbol);
                    }
                    else
                    {
                        AddToken("<", line, TokenType.Symbol);
                    }
                    return true;
                case '>':
                    if (Lookup() == '=')
                    {
                        NextToken();
                        AddToken(">=", line, TokenType.Symbol);
                    }
                    else
                    {
                        AddToken(">", line, TokenType.Symbol);
                    }
                    return true;
                case '*':
                    if (Lookup() == '*')
                    {
                        NextToken();
                        AddToken("**", line, TokenType.Symbol);
                    }
                    else
                    {
                        AddToken("*", line, TokenType.Symbol);
                    }
                    return true;
                case '/':
                    if (Lookup() == '/')
                    {
                        NextToken();
                        AddToken("//", line, TokenType.Symbol);
                    }
                    else
                    {
                        AddToken("/", line, TokenType.Symbol);
                    }
                    return true;
                case '=':
                    if (Lookup() == '=')
                    {
                        NextToken();
                        AddToken("==", line, TokenType.Symbol);
                    }
                    else
                    {
                        AddToken("=", line, TokenType.Symbol);
                    }
                    return true;

                //whitespace
                case ' ':
                case '\r':
                case '\t':
                    break;

                //comments
                case '#':
                    while (Lookup(1) != '\n') NextToken();
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
                        return true;
                    }

                default:
                    return false;
            }

            return true;
        }

        void AddToken(string lexeme, int line, TokenType type)
        {
            tokens.Add(new(lexeme, line, type));
        }

        char Lookup(int n = 0)
        {
            int nextCharPos = currentChar + n;
            return AtEnd || nextCharPos >= sourceLength ? '\0' : source[nextCharPos];
        }

        char NextToken()
        {
            return source[currentChar++];
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
                "<", ">", "<=", ">=", "=="
            ];
        }
    }
}
