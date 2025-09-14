using System.Globalization;
using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Compiler.Syntax;

public class Lexer(string source, string fileName)
{
    readonly string source = source;
    readonly string fileName = fileName;

    bool AtEnd => currentCharIndex >= source.Length;

    readonly List<Token> tokens = [];
    readonly List<IGlykonError> errors = [];
    int line = 0;
    int currentCharIndex = 0;

    public (Token[] tokens, List<IGlykonError> errors) Execute()
    {
        while (!AtEnd)
        {
            Token token = GetNextToken();
            if (!token.IsEmpty)
            {
                tokens.Add(token);
            }
        }

        if (tokens.Count > 0)
        {
            var last = ScanEndOfLine();

            if (!last.IsEmpty)
            {
                tokens.Add(last);
            }

            tokens.Add(new(TokenKind.EOF, line));
        }

        return (tokens.ToArray(), errors);
    }

    Token GetNextToken()
    {
        char character = Advance();

        switch (character)
        {
            //symbols
            case '{':
                return new(TokenKind.BraceLeft, line);
            case '}':
                return new(TokenKind.BraceRight, line);
            case '(':
                return new(TokenKind.ParenthesisLeft, line);
            case ')':
                return new(TokenKind.ParenthesisRight, line);
            case '[':
                return new(TokenKind.BracketLeft, line);
            case ']':
                return new(TokenKind.BracketRight, line);
            case ',':
                return new(TokenKind.Comma, line);
            case ':':
                return new(TokenKind.Colon, line);

            //long symbols
            case '.':
                {
                    if (char.IsAsciiDigit(Peek()))
                    {
                        return ScanNumber(true);
                    }

                    return new(TokenKind.Dot, line);
                }
            case '+':
                return new(TokenKind.Plus, line);
            case '-':
                if (Match('>'))
                {
                    return new(TokenKind.Arrow, line);
                }

                return new(TokenKind.Minus, line);
            case '<':
                return new(Match('=') ? TokenKind.LessEqual : TokenKind.Less, line);
            case '>':
                return new(Match('=') ? TokenKind.GreaterEqual : TokenKind.Greater, line);
            case '*':
                return new(Match('*') ? TokenKind.StarDouble : TokenKind.Star, line);
            case '/':
                return new(Match('/') ? TokenKind.SlashDouble : TokenKind.Slash, line);
            case '=':
                return new(Match('=') ? TokenKind.Equal : TokenKind.Assignment, line);
            case '!': //! is not valid by itself
                if (Match('='))
                {
                    return new(TokenKind.NotEqual, line);
                }
                else
                {
                    errors.Add(new SyntaxError(line, fileName, $"Invalid character '{character}' in token"));
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
                while (!AtEnd && Peek() != '\n') Advance();
                break;

            //statement terminator
            case '\n':
                line++;
                return ScanEndOfLine();
            case ';':
                return new(TokenKind.Semicolon, line);

            default:
                if (char.IsAsciiDigit(character))
                {
                    return ScanNumber();
                }

                if (IsAllowedIdentifierStartCharacter(character))
                {
                    return ScanIdentifier();
                }

                errors.Add(new SyntaxError(line, fileName, $"Invalid character '{character}' in token"));
                return Token.Empty;
        }

        return Token.Empty;
    }

    Token ScanIdentifier()
    {
        int identifierStart = currentCharIndex - 1;

        while (IsAllowedIdentifierCharacter(Peek())) Advance();

        string identifier = source[identifierStart..currentCharIndex];

        if (keywords.TryGetValue(identifier, out TokenKind type))
        {
            return new(type, line);
        }

        return new(TokenKind.Identifier, line, identifier);
    }

    Token ScanNumber(bool isReal = false)
    {
        int numberStart = currentCharIndex - 1;

        while (char.IsAsciiDigit(Peek())) Advance();

        if (Peek() == '.' && char.IsAsciiDigit(Peek(1)))
        {
            isReal = true;

            Advance();

            while (char.IsAsciiDigit(Peek())) Advance();
        }

        string number = source[numberStart..currentCharIndex];

        if (isReal)
        {
            return new(TokenKind.LiteralReal, line, double.Parse(number, CultureInfo.InvariantCulture));
        }

        return new(TokenKind.LiteralInt, line, int.Parse(number));
    }

    Token ScanString(char openingQuote)
    {
        bool multiline = Match(openingQuote, 2);
        int currentLine = line;
        int stringStart = currentCharIndex;

        while (!AtEnd && !(multiline ? Match(openingQuote, 3) : Match(openingQuote)))
        {
            if (Peek() == '\n')
            {
                if (!multiline)
                {
                    errors.Add(new SyntaxError(line, fileName, "SyntaxError: unterminated string literal"));
                    return Token.Empty;
                }

                line++;
            }

            Advance();
        }

        if (AtEnd)
        {
            errors.Add(new SyntaxError(line, fileName, "SyntaxError: unterminated string literal"));
            return Token.Empty;
        }

        int stringEndOffset = multiline ? 3 : 1;
        Token result = new(TokenKind.LiteralString, currentLine, source[stringStart..(currentCharIndex - stringEndOffset)]);

        return result;
    }

    Token ScanEndOfLine()
    {
        Token last = tokens.LastOrDefault();

        if (last.IsEmpty || terminatorExceptions.Contains(last.Kind)) return Token.Empty;

        (char nextChar, int peekIndex) = PeekNextSignificant();
        if (chainingChars.Contains(nextChar)) return Token.Empty;

        // Handle long chaining tokens
        if (IsLongChainingToken(nextChar, peekIndex)) return Token.Empty;

        return new(TokenKind.Semicolon, line);
    }

    bool Match(char token)
    {
        if (AtEnd || Peek() != token) return false;
        currentCharIndex++;
        return true;
    }

    bool Match(char token, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            if (AtEnd || Peek(i) != token) return false;
        }

        currentCharIndex += offset;

        return true;
    }

    char Peek(int offset = 0)
    {
        int nextCharPos = currentCharIndex + offset;
        char c = AtEnd || nextCharPos >= source.Length ? '\0' : source[nextCharPos];
        return c;
    }

    (char, int) PeekNextSignificant()
    {
        int i = currentCharIndex;

        while (i < source.Length)
        {
            char c = source[i];

            if (!char.IsWhiteSpace(c))
            {
                return (c, i);
            }
            i++;
        }

        return ('\0', -1);
    }

    bool IsLongChainingToken(char c, int index)
    {
        // Check for end of source
        if (index == -1) return false;

        // Handle 'and'
        if (c == 'a')
        {
            const int len = 3;
            int end = index + len;

            if (source.Length >= end && source.Substring(index, len) == "and")
            {
                if (source.Length == end || !IsAllowedIdentifierCharacter(source[end]))
                {
                    return true;
                }
            }
        }
        // Handle 'or'
        else if (c == 'o')
        {
            const int len = 2;
            int end = index + len;

            if (source.Length >= end && source.Substring(index, len) == "or")
            {
                if (source.Length == end || !IsAllowedIdentifierCharacter(source[end]))
                {
                    return true;
                }
            }
        }
        // Handle 'not'
        else if (c == 'n')
        {
            const int len = 3;
            int end = index + len;

            if (source.Length >= end && source.Substring(index, len) == "not")
            {
                if (source.Length == end || !IsAllowedIdentifierCharacter(source[end]))
                {
                    return true;
                }
            }
        }

        return false;
    }

    char Advance()
    {
        return source[currentCharIndex++];
    }

    static bool IsAllowedIdentifierStartCharacter(char c)
    {
        return char.IsLetter(c) || c == '_' || c == '@';
    }

    static bool IsAllowedIdentifierCharacter(char c)
    {
        return c == '_' || char.IsLetterOrDigit(c);
    }

    readonly static Dictionary<string, TokenKind> keywords;

    readonly static HashSet<TokenKind> terminatorExceptions;
    readonly static HashSet<char> chainingChars;

    static Lexer()
    {
        keywords = new()
            {
                { "class", TokenKind.Class },
                { "struct", TokenKind.Struct },
                { "interface", TokenKind.Interface },
                { "enum", TokenKind.Enum },
                { "def", TokenKind.Def },
                { "let", TokenKind.Let },
                { "const", TokenKind.Const },
                { "int", TokenKind.Int },
                { "real", TokenKind.Real },
                { "str", TokenKind.String },
                { "bool",  TokenKind.Bool },
                { "true",  TokenKind.LiteralTrue },
                { "false",  TokenKind.LiteralFalse },
                { "none", TokenKind.None },
                { "and", TokenKind.And },
                { "not", TokenKind.Not },
                { "or", TokenKind.Or },
                { "if", TokenKind.If },
                { "else", TokenKind.Else },
                { "elif", TokenKind.Elif },
                { "for", TokenKind.For },
                { "while", TokenKind.While },
                { "return", TokenKind.Return },
                { "break", TokenKind.Break },
                { "continue", TokenKind.Continue }
            };

        terminatorExceptions =
    [
    // Internal/special
    TokenKind.EOF, TokenKind.Semicolon,

    // Openers
    TokenKind.BracketLeft,
    TokenKind.ParenthesisLeft,
    TokenKind.BraceLeft,

    // Block closer
    TokenKind.BraceRight,

    // Punctuation and operators that precede an operand
    TokenKind.Comma,
    TokenKind.Colon,
    TokenKind.Arrow,
    TokenKind.Dot,
    TokenKind.Assignment,
    
    // Arithmetic operators
    TokenKind.Plus,
    TokenKind.Minus,
    TokenKind.Star,
    TokenKind.StarDouble,
    TokenKind.Slash,
    TokenKind.SlashDouble,

    // Comparison operators
    TokenKind.Equal,
    TokenKind.NotEqual,
    TokenKind.Greater,
    TokenKind.GreaterEqual,
    TokenKind.Less,
    TokenKind.LessEqual,
    
    // Logical operators
    TokenKind.And,
    TokenKind.Or,
    TokenKind.Not,

    // Declaration and control-flow keywords
    TokenKind.Def,
    TokenKind.Class,
    TokenKind.Struct,
    TokenKind.Interface,
    TokenKind.Enum,
    TokenKind.Let,
    TokenKind.Const,
    TokenKind.If,
    TokenKind.Else,
    TokenKind.Elif,
    TokenKind.For,
    TokenKind.While
    ];

        chainingChars = ['.', '[', '(', '+', '-', '*', '/', '=', '!', '<', '>'];
    }
}
