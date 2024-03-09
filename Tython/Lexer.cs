using Tython.Model;

namespace Tython
{
    public class Lexer(string source, string fileName)
    {
        readonly string source = source;
        readonly string fileName = fileName;

        List<Token> tokens = [];
        int line = 1;
        bool multilineComment = false;

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
                "{", "}", "(", ")", "[", "]", "=", ":",
                ".", ",", "+", "-", "*", "/", "**", "//",
                "<", ">", "<=", ">=", "=="
            ];
        }

        public List<Token> ScanSource()
        {
            string[] sourceLines = source.Split("\n");

            for (int i = 0; i < sourceLines.Length; i++)
            {
                if (multilineComment)
                {
                    line++;
                    continue;
                }

                string sourceLine = RemoveComments(sourceLines[i].Trim());
                if (string.IsNullOrEmpty(sourceLine))
                {
                    line++;
                    continue;
                }
            }

            return tokens;
        }

        string RemoveComments(string line)
        {
            //sets field to true when a multiline comment begins, sets to false when it ends
            if (line.Contains("##"))
            {
                multilineComment ^= true;
                return string.Empty;
            }

            if (line.Contains('#')) return string.Empty;

            return line;
        }
    }
}
