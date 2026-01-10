using Glykon.Compiler.Diagnostics.Exceptions;
using Glykon.Compiler.Syntax;

namespace Glykon.Compiler.Diagnostics.Errors
{
    public class ParseError(Token? token, string filename, string message) : IGlykonError
    {
        private readonly Token? token = token;
        private readonly string filename = filename;
        private readonly string message = message;

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
