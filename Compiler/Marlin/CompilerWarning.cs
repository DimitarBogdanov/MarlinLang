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

        public Level WarningLevel { get; private set; }
        public string Message { get; private set; }
        public Token RootCause { get; private set; }

        public CompilerWarning(Level level, string message, Token rootCause)
        {
            WarningLevel = level;
            Message = message;
            RootCause = rootCause;
        }
    }
}
