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
        public class ErrorCode
        {
            public const string UNKNOWN_TOKEN = "MAR0001";
            public const string EXPECTED_ROOT_MEMBER = "MAR0002";
            public const string NAME_MUST_BE_IDENTIFIER = "MAR0003";
            public const string EXPECTED_CLASS_MEMBER = "MAR0004";
            public const string EXPECTED_SCOPE = "MAR0005";
            public const string UNEXPECTED_EOF = "MAR0006";
            public const string CANNOT_HAVE_PAREN_AFTER_COMMA_ARGLIST = "MAR0007";
            public const string EXPECTED_COMMA_ARGLIST = "MAR0008";
            public const string EXPECTED_PAREN_CLOSE_ARGLIST = "MAR0009";
            public const string EXPECTED_FUNCTION_MEMBER = "MAR0010";
            public const string EXPECTED_STATEMENT = "MAR0011";
            public const string EXPECTED_EXPRESSION = "MAR0012";
            public const string EXPECTED_ANON_SCOPE_MEMBER = "MAR0013";
            public const string MISSING_SEMICOLON = "MAR0014";
            public const string EXPECTED_FUNC_CALL_OR_VAR_ASSIGN = "MAR0015";
            public const string UNEXPECTED_SEMICOLON = "MAR0016";
            public const string MISSING_OPERATOR_RIGHT = "MAR0017";
        }

        public Level WarningLevel { get; private set; }
        public Source WarningSource { get; private set; }
        public string Code { get; private set; }
        public string Message { get; private set; }
        public Token RootCause { get; private set; }

        public CompilerWarning(Level level, Source source, string code, string message, Token rootCause)
        {
            if (source == Source.LEXER && level != Level.ERROR)
            {
                throw new System.Exception("lexer returned a " + level.ToString());
            }

            WarningLevel = level;
            WarningSource = source;
            Code = code;
            Message = message;
            RootCause = rootCause;
        }
    }
}
