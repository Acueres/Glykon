using TythonCompiler.Diagnostics.Exceptions;
using TythonCompiler.Tokenization;

namespace TythonCompiler.Diagnostics.Errors
{
    public class ParseError(Token? token, string filename, string message) : ITythonError
    {
        readonly Token? token = token;
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}, line {token?.Line})");
        }

        public ParseException Exception()
        {
            return new(message);
        }
    }
}
