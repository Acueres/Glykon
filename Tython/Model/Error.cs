namespace Tython.Model
{
    public interface ITythonError
    {
        void Report();
    }

    public class SyntaxError(int line, string filename, string message) : ITythonError
    {
        readonly int line = line;
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}, line {line})");
        }
    }

    public class ParseError(Token token, string filename, string message) : ITythonError
    {
        readonly Token token = token;
        readonly string filename = filename;
        readonly string message = message;

        public void Report()
        {
            Console.WriteLine($"{message} ({filename}, line {token.Line})");
        }

        public ParseException Exception()
        {
            return new(message);
        }
    }

    public class ParseException : Exception
    {
        public ParseException() { }
        public ParseException(string message) : base(message) { }
        public ParseException(string message, Exception inner) : base(message, inner) { }
    }
}
