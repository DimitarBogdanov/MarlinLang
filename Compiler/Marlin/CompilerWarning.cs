/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     CompilerWarning.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

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
            PARSER,
            SEMANTIC_ANALYSIS
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
            public const string EXPECTED_PAREN_CLOSE_EXPR = "MAR0018";
            public const string SYMBOL_ALREADY_EXISTS = "MAR0019";
            public const string UNKNOWN_SYMBOL = "MAR0020";
            public const string EXPECTED_SET_SEMICOLON_DECLARE_VAR = "MAR0021";
            public const string EXPECTED_IDENTIFIER_IN_TYPE_NAME = "MAR0022";
            public const string VARIABLE_USED_BEFORE_DECLARATION = "MAR0023";
            public const string ARGUMENT_MISMATCH = "MAR0024";
            public const string VOID_MISUSE = "MAR0025";
        }

        public Level WarningLevel { get; private set; }
        public Source WarningSource { get; private set; }
        public string Code { get; private set; }
        public string Message { get; private set; }
        public string File { get; private set; }
        public Token RootCause { get; private set; }

        public CompilerWarning(Level level, Source source, string code, string message, string file, Token rootCause)
        {
            WarningLevel = level;
            WarningSource = source;
            Code = code;
            Message = message;
            File = file;
            RootCause = rootCause;
        }
    }
}
