using Marlin.Lexing;

namespace Marlin
{
    public class CompilerWarning
    {
        public enum Level
        {
            MESSAGE,
            WARNING,
            ERROR
        }
        public enum Source
        {
            LEXER,
            PARSER
        }

        public Level WarningLevel { get; private set; }
        public Source WarningSource { get; private set; }
        public string Message { get; private set; }
        public Token RootCause { get; private set; }

        public CompilerWarning(Level level, Source source, string message, Token rootCause)
        {
            WarningLevel = level;
            Message = message;
            RootCause = rootCause;
            System.Console.WriteLine("Err " + Message);
        }
    }
}
