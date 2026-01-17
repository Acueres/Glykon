using Glykon.Compiler.Core;
using Glykon.Compiler.Diagnostics.Errors;

namespace Glykon.Compiler.Syntax;

public class Lexer(SourceText source, string fileName, SyntaxMode mode)
{
    private bool AtEnd => currentCharIndex >= source.Length;

    private readonly List<Token> tokens = [];
    private readonly List<IGlykonError> errors = [];
    private int line;
    private int currentCharIndex;

    public LexResult Lex()
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
            if (mode == SyntaxMode.Predict)
            {
                tokens.Add(new Token(TokenKind.Cursor, line));
            }

            tokens.Add(new Token(TokenKind.EOF, line));
        }

        var processedTokens = InsertTerminators();

        return new LexResult(processedTokens, [..errors]);
    }

    private Token GetNextToken()
    {
        char character = Advance();

        switch (character)
        {
            //symbols
            case '{':
                return new Token(TokenKind.BraceLeft, line);
            case '}':
                return new Token(TokenKind.BraceRight, line);
            case '(':
                return new Token(TokenKind.ParenthesisLeft, line);
            case ')':
                return new Token(TokenKind.ParenthesisRight, line);
            case '[':
                return new Token(TokenKind.BracketLeft, line);
            case ']':
                return new Token(TokenKind.BracketRight, line);
            case ',':
                return new Token(TokenKind.Comma, line);
            case ':':
                return new Token(TokenKind.Colon, line);

            //long symbols
            case '.':
            {
                if (char.IsAsciiDigit(Peek()))
                {
                    return ScanNumber(true);
                }

                if (Match('.'))
                {
                    if (Match('=')) return new Token(TokenKind.RangeInclusive, line);
                    return new Token(TokenKind.Range, line);
                }

                return new Token(TokenKind.Dot, line);
            }
            case '+':
                return new Token(TokenKind.Plus, line);
            case '-':
                if (Match('>'))
                {
                    return new Token(TokenKind.Arrow, line);
                }

                return new Token(TokenKind.Minus, line);
            case '<':
                return new Token(Match('=') ? TokenKind.LessEqual : TokenKind.Less, line);
            case '>':
                return new Token(Match('=') ? TokenKind.GreaterEqual : TokenKind.Greater, line);
            case '*':
                return new Token(Match('*') ? TokenKind.StarDouble : TokenKind.Star, line);
            case '/':
                return new Token(Match('/') ? TokenKind.SlashDouble : TokenKind.Slash, line);
            case '=':
                return new Token(Match('=') ? TokenKind.Equal : TokenKind.Assignment, line);
            case '!': //! is not valid by itself
                if (Match('='))
                {
                    return new Token(TokenKind.NotEqual, line);
                }

                errors.Add(new SyntaxError(line, fileName, $"Invalid character '{character}' in token"));

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

            case '\n':
                // Increase line at newline
                return new Token(TokenKind.Newline, line++);
            //statement terminator
            case ';':
                return new Token(TokenKind.Semicolon, line);

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
                break;
        }

        return Token.Empty;
    }

    private Token ScanIdentifier()
    {
        int identifierStart = currentCharIndex - 1;

        while (IsAllowedIdentifierCharacter(Peek())) Advance();

        TextSpan identifier = new(source, identifierStart, currentCharIndex - identifierStart);

        if (keywords.TryGetValue(identifier.Text, out TokenKind type))
        {
            return new Token(type, line);
        }

        return new Token(TokenKind.Identifier, line, identifier);
    }

    private Token ScanNumber(bool isReal = false)
    {
        int numberStart = currentCharIndex - 1;

        while (char.IsAsciiDigit(Peek())) Advance();

        if (Peek() == '.' && char.IsAsciiDigit(Peek(1)))
        {
            isReal = true;

            Advance();

            while (char.IsAsciiDigit(Peek())) Advance();
        }

        TextSpan number = new(source, numberStart, currentCharIndex - numberStart);

        TokenKind type = TokenKind.LiteralInt;
        if (isReal)
        {
            type = TokenKind.LiteralReal;
        }

        return new Token(type, line, number);
    }

    private Token ScanString(char openingQuote)
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
                    errors.Add(new SyntaxError(line, fileName, "Unterminated string literal"));
                    return Token.Empty;
                }

                line++;
            }

            Advance();
        }

        if (AtEnd)
        {
            errors.Add(new SyntaxError(line, fileName, "Unterminated string literal"));
            return Token.Empty;
        }

        int stringEndOffset = multiline ? 3 : 1;
        Token result = new(TokenKind.LiteralString, currentLine,
            new TextSpan(source, stringStart, currentCharIndex - stringStart - stringEndOffset));

        return result;
    }

    private Token[] InsertTerminators()
    {
        List<Token> processedTokens = [];
        
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];
            if (token.Kind != TokenKind.Newline)
            {
                processedTokens.Add(token);
                continue;
            }
            
            if (i == 0) continue;
            
            var previousToken = tokens[i - 1];
            
            if (asiNoInsertAfter.Contains(previousToken.Kind)) continue;

            if (i == tokens.Count - 1) continue;
            
            var nextToken = tokens[i + 1];
            
            if (asiContinuationBefore.Contains(nextToken.Kind)) continue;
            
            processedTokens.Add(new Token(TokenKind.Semicolon, token.Line));
        }
        
        return processedTokens.ToArray();
    }

    private bool Match(char token)
    {
        if (AtEnd || Peek() != token) return false;
        currentCharIndex++;
        return true;
    }

    private bool Match(char token, int offset)
    {
        for (int i = 0; i < offset; i++)
        {
            if (AtEnd || Peek(i) != token) return false;
        }

        currentCharIndex += offset;

        return true;
    }

    private char Peek(int offset = 0)
    {
        int nextCharPos = currentCharIndex + offset;
        char c = AtEnd || nextCharPos >= source.Length ? '\0' : source[nextCharPos];
        return c;
    }

    private char Advance()
    {
        return source[currentCharIndex++];
    }

    private static bool IsAllowedIdentifierStartCharacter(char c)
    {
        return char.IsLetter(c) || c == '_' || c == '@';
    }

    private static bool IsAllowedIdentifierCharacter(char c)
    {
        return c == '_' || char.IsLetterOrDigit(c);
    }

    private static readonly Dictionary<string, TokenKind> keywords;
    private static readonly HashSet<TokenKind> asiNoInsertAfter;
    private static readonly HashSet<TokenKind> asiContinuationBefore;

    static Lexer()
    {
        keywords = new Dictionary<string, TokenKind>
        {
            { "class", TokenKind.Class },
            { "struct", TokenKind.Struct },
            { "interface", TokenKind.Interface },
            { "new", TokenKind.New },
            { "enum", TokenKind.Enum },
            { "def", TokenKind.Def },
            { "let", TokenKind.Let },
            { "const", TokenKind.Const },
            { "true", TokenKind.LiteralTrue },
            { "false", TokenKind.LiteralFalse },
            { "none", TokenKind.None },
            { "and", TokenKind.And },
            { "not", TokenKind.Not },
            { "or", TokenKind.Or },
            { "if", TokenKind.If },
            { "else", TokenKind.Else },
            { "elif", TokenKind.Elif },
            { "for", TokenKind.For },
            { "by", TokenKind.By },
            { "in", TokenKind.In },
            { "as", TokenKind.As },
            { "while", TokenKind.While },
            { "return", TokenKind.Return },
            { "break", TokenKind.Break },
            { "continue", TokenKind.Continue }
        };

        asiNoInsertAfter =
        [
            // Internal/special
            TokenKind.EOF,
            // Avoid double terminators
            TokenKind.Semicolon,
            TokenKind.Newline,

            // Openers
            TokenKind.BracketLeft,
            TokenKind.ParenthesisLeft,
            TokenKind.BraceLeft,

            // Block closer
            TokenKind.BraceRight,

            // Punctuation and operators that precede an operand
            TokenKind.Comma,
            TokenKind.Colon,
            TokenKind.Dot,
            TokenKind.Assignment,
            TokenKind.Arrow,
            TokenKind.Range,
            TokenKind.RangeInclusive,

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

            // Declarations
            TokenKind.Def,
            TokenKind.Class,
            TokenKind.Struct,
            TokenKind.Interface,
            TokenKind.Enum,
            TokenKind.Let,
            TokenKind.Const,
            TokenKind.New,

            // Control-flow keywords
            TokenKind.If,
            TokenKind.Else,
            TokenKind.Elif,

            // Other keywords
            TokenKind.For,
            TokenKind.By,
            TokenKind.In,
            TokenKind.While,
            TokenKind.As
        ];

        asiContinuationBefore =
        [
            TokenKind.Cursor,

            // Postfix / delimiters
            TokenKind.Dot,
            TokenKind.ParenthesisLeft,
            TokenKind.ParenthesisRight,
            TokenKind.BracketLeft,
            TokenKind.BracketRight,

            // Arithmetic operators
            TokenKind.Plus,
            TokenKind.Minus,
            TokenKind.Star,
            TokenKind.StarDouble,
            TokenKind.Slash,
            TokenKind.SlashDouble,

            // Assignment / comparison
            TokenKind.Assignment,
            TokenKind.Equal,
            TokenKind.NotEqual,
            TokenKind.Less,
            TokenKind.LessEqual,
            TokenKind.Greater,
            TokenKind.GreaterEqual,

            // Logical operators
            TokenKind.And,
            TokenKind.Not,
            TokenKind.Or,

            // Keyword operator
            TokenKind.As,
        ];
    }
}
