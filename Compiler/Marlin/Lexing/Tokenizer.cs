﻿using System;
using System.IO;

namespace Marlin.Lexing
{
    public class Tokenizer
    {
        private readonly string path;
        private readonly TokenStream tokens;

        public Tokenizer(string path)
        {
            this.path = path;
            tokens = new();
        }

        public TokenStream Tokenize()
        {
            bool inIdentifier = false;
            bool inString = false;
            bool inInteger = false;
            bool inDecimal = false;
            bool inLineComment = false;
            bool inBlockComment = false;

            int line = 1;
            int col = 0;

            string current = "";

            using StreamReader reader = new(path);
            while (reader.Peek() >= 0)
            {
                char currentChar = (char)reader.Read();

                if (currentChar != '\r')
                {
                    col++;
                }

                if (currentChar == '\n')
                {
                    line++;
                    col = 0;
                    if (inLineComment)
                        inLineComment = false;
                }

                if (inLineComment)
                {
                    continue;
                }
                else if (inBlockComment)
                {
                    if (currentChar == '*' && reader.Peek() == '/')
                    {
                        reader.Read();
                        inBlockComment = false;
                        continue;
                    }
                }

            REPROCESS_CURRENT:
                if (inIdentifier)
                {
                    if (currentChar == '_' || char.IsLetterOrDigit(currentChar))
                    {
                        current += currentChar;
                        continue;
                    }
                    else
                    {
                        tokens.Add(new(GetIdentifierType(current), current, line, (col - current.Length)));
                        inIdentifier = false;
                        current = "";
                        goto REPROCESS_CURRENT;
                    }
                }
                else if (inString)
                {
                    if (currentChar == '"')
                    {
                        tokens.Add(new(TokenType.STRING, current, line, (col - current.Length)));
                        inString = false;
                        current = "";
                        continue;
                    }
                    else
                    {
                        current += currentChar;
                        continue;
                    }
                }
                else if (inInteger)
                {
                    if (char.IsDigit(currentChar))
                    {
                        current += currentChar;
                        continue;
                    }
                    else if (currentChar == '.')
                    {
                        inInteger = false;
                        inDecimal = true;
                        current += ".";
                    }
                    else
                    {
                        tokens.Add(new(TokenType.INTEGER, current, line, (col - current.Length)));
                        inInteger = false;
                        current = "";
                        goto REPROCESS_CURRENT;
                    }
                }
                else if (inDecimal)
                {
                    if (char.IsDigit(currentChar))
                    {
                        current += currentChar;
                        continue;
                    }
                    else
                    {
                        tokens.Add(new(TokenType.DECIMAL, current, line, (col - current.Length)));
                        inInteger = false;
                        current = "";
                        goto REPROCESS_CURRENT;
                    }
                }
                else
                {
                    switch (currentChar)
                    {
                        case '(':
                            tokens.Add(new(TokenType.PAREN_LEFT, "(", line, col));
                            continue;
                        case ')':
                            tokens.Add(new(TokenType.PAREN_RIGHT, ")", line, col));
                            continue;
                        case '{':
                            tokens.Add(new(TokenType.BRACE_LEFT, "{", line, col));
                            continue;
                        case '}':
                            tokens.Add(new(TokenType.BRACE_RIGHT, "}", line, col));
                            continue;

                        case '+':
                            tokens.Add(new(TokenType.PLUS, "+", line, col));
                            continue;
                        case '-':
                            tokens.Add(new(TokenType.MINUS, "-", line, col));
                            continue;
                        case '^':
                            tokens.Add(new(TokenType.POWER, "^", line, col));
                            continue;

                        case '*':
                            tokens.Add(new(TokenType.MULTIPLY, "*", line, col));
                            continue;

                        case '/':
                            {
                                if ((char)reader.Peek() == '/')
                                {
                                    reader.Read();
                                    inLineComment = true;
                                    continue;
                                }
                                else if ((char)reader.Peek() == '*')
                                {
                                    reader.Read();
                                    inBlockComment = true;
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.DIVIDE, "/", line, col));
                                    continue;
                                }
                            }

                        case '.':
                            {
                                if (char.IsDigit((char)reader.Peek()))
                                {
                                    inDecimal = true;
                                    current = "0.";
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.DOT, ".", line, col));
                                    continue;
                                }
                            }

                        case ',':
                            tokens.Add(new(TokenType.COMMA, ",", line, col));
                            continue;
                        case ':':
                            tokens.Add(new(TokenType.COLON, ":", line, col));
                            continue;
                        case ';':
                            tokens.Add(new(TokenType.SEMICOLON, ";", line, col));
                            continue;

                        case '!':
                            {
                                if (reader.Peek() == '=')
                                {
                                    reader.Read();
                                    tokens.Add(new(TokenType.NOT_EQUAL, "!=", line, col - 1));
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.BANG, "!", line, col - 1));
                                    continue;
                                }
                            }

                        case '=':
                            {
                                if (reader.Peek() == '=')
                                {
                                    reader.Read();
                                    tokens.Add(new(TokenType.EQUALS, "==", line, col - 1));
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.SET, "=", line, col));
                                    continue;
                                }
                            }

                        case '>':
                            {
                                if (reader.Peek() == '=')
                                {
                                    reader.Read();
                                    tokens.Add(new(TokenType.GREATER_EQUAL, ">=", line, col - 1));
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.GREATER, ">", line, col - 1));
                                    continue;
                                }
                            }

                        case '<':
                            {
                                if (reader.Peek() == '=')
                                {
                                    reader.Read();
                                    tokens.Add(new(TokenType.LESS_EQUAL, "<=", line, col - 1));
                                    continue;
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.LESS, "<", line, col));
                                    continue;
                                }
                            }

                        default:
                            {
                                if (inBlockComment)
                                    continue;

                                if (char.IsLetter(currentChar) || currentChar == '_')
                                {
                                    inIdentifier = true;
                                    goto REPROCESS_CURRENT;
                                }
                                else if (currentChar == '"')
                                {
                                    inString = true;
                                    continue;
                                }
                                else if (char.IsDigit(currentChar))
                                {
                                    inInteger = true;
                                    goto REPROCESS_CURRENT;
                                }
                                else if (char.IsWhiteSpace(currentChar))
                                {
                                    if (inIdentifier)
                                    {
                                        tokens.Add(new(GetIdentifierType(current), current, line, (col - current.Length)));
                                        current = "";
                                        inIdentifier = false;
                                        continue;
                                    }
                                    else if (inString)
                                    {
                                        current += currentChar;
                                        continue;
                                    }
                                    else if (inInteger)
                                    {
                                        tokens.Add(new(TokenType.INTEGER, current, line, (col - current.Length)));
                                        current = "";
                                        inInteger = false;
                                        continue;
                                    }
                                    else if (inDecimal)
                                    {
                                        tokens.Add(new(TokenType.DECIMAL, current, line, (col - current.Length)));
                                        current = "";
                                        inDecimal = false;
                                        continue;
                                    }
                                    else
                                    {
                                        continue;
                                    }
                                }
                                else
                                {
                                    tokens.Add(new(TokenType.UNKNOWN, currentChar.ToString(), line, col));
                                    continue;
                                }
                            }
                    }
                }
            }

            // EOF
            if (inIdentifier)
            {
                tokens.Add(new(GetIdentifierType(current), current, line, (col - current.Length)));
            }
            else if (inString)
            {
                // TODO errors
                throw new("Unfinished string");
            }
            else if (inInteger)
            {
                tokens.Add(new(TokenType.INTEGER, current, line, (col - current.Length)));
            }
            else if (inDecimal)
            {
                tokens.Add(new(TokenType.DECIMAL, current, line, (col - current.Length)));
            }

            tokens.Add(new(TokenType.EOF, "<EOF>", line, col));

            return tokens;
        }

        private static TokenType GetIdentifierType(string identifier)
        {
            return identifier switch
            {
                "true"       =>  TokenType.BOOLEAN,
                "false"      =>  TokenType.BOOLEAN,
                
                "and"        =>  TokenType.AND,
                "or"         =>  TokenType.OR,
                
                "class"      =>  TokenType.CLASS,
                "func"       =>  TokenType.FUNCTION,

                _            =>  TokenType.IDENTIFIER,
            };
        }
    }
}