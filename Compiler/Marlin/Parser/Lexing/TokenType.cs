namespace Marlin.Parser.Lexing
{
    public enum TokenType
    {
        UNKNOWN,        // ?
        EOF,            // <EOF>

        IDENTIFIER,     // SomeName
        INTEGER,        // 123
        DECIMAL,        // 12.34
        BOOLEAN,        // true
        STRING,         // "Hello, world!"

        PAREN_LEFT,     // (
        PAREN_RIGHT,    // )
        BRACE_LEFT,     // {
        BRACE_RIGHT,    // }

        PLUS,           // +
        MINUS,          // -
        POWER,          // ^
        MULTIPLY,       // *
        DIVIDE,         // /

        DOT,            // .
        COMMA,          // ,
        COLON,          // :
        SEMICOLON,      // ;

        BANG,           // !
        SET,            // =
        EQUALS,         // ==
        NOT_EQUAL,      // !=

        GREATER,        // >
        LESS,           // <
        GREATER_EQUAL,  // >=
        LESS_EQUAL,     // <=

        AND,            // &&
        OR,             // ||

        CLASS,          // class
        FUNCTION,       // function
    }
}
