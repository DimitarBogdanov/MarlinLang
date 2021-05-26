/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     TokenType.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

namespace Marlin.Lexing
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
        DOUBLE_COLON,   // ::
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
        FUNCTION,       // func
        CONSTRUCTOR,    // constructor
        ATTRIBUTE,      // static
        NEW,            // new
        RETURN,         // return
    }
}
