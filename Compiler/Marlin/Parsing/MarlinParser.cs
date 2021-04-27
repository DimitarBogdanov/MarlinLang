/*
 * Copyright (C) Dimitar Bogdanov
 * Filename:     MarlinParser.cs
 * Project:      Marlin Compiler
 * License:      Creative Commons Attribution NoDerivs (CC-ND)
 * 
 * Refer to the "LICENSE" file, or to the following link:
 * https://creativecommons.org/licenses/by-nd/3.0/
 */

using Marlin.Lexing;
using System;
using System.Collections.Generic;
using System.Globalization;
using static Marlin.CompilerWarning;

namespace Marlin.Parsing
{
    public class MarlinParser
    {
        private readonly TokenStream stream;
        private readonly string file;
        public List<CompilerWarning> warnings = new();
        public static long totalParseTime = 0;

        public MarlinParser(TokenStream stream, string file)
        {
            this.stream = stream;
            this.file = file;
        }

        private static bool IsBinaryOperator(TokenType type)
        {
            return type switch
            {
                   TokenType.DIVIDE
                or TokenType.EQUALS
                or TokenType.GREATER
                or TokenType.GREATER_EQUAL
                or TokenType.LESS
                or TokenType.LESS_EQUAL
                or TokenType.MULTIPLY
                or TokenType.NOT_EQUAL
                or TokenType.OR
                or TokenType.AND
                or TokenType.PLUS
                or TokenType.MINUS
                or TokenType.POWER
                  => true,

                _ => false
            };
        }

        private static bool IsAssignmentOperator(TokenType type)
        {
            // TODO: += -= ++ --
            return type switch
            {
                   TokenType.SET
                  => true,

                _ => false
            };
        }

        private static int GetTokenPrecedence(TokenType type)
        {
            return type switch
            {
                TokenType.POWER          => 90,
                TokenType.MULTIPLY       => 70,
                TokenType.DIVIDE         => 70,
                TokenType.PLUS           => 60,
                TokenType.MINUS          => 60,

                TokenType.EQUALS         => 200,
                TokenType.GREATER        => 100,
                TokenType.GREATER_EQUAL  => 100,
                TokenType.LESS           => 100,
                TokenType.LESS_EQUAL     => 100,
                TokenType.NOT_EQUAL      => 100,
                TokenType.OR             => 100,
                TokenType.AND            => 100,

                _ => -1
            };
        }

        public Node Parse()
        {
            long start = Program.CurrentTimeMillis();
            Node rootNode = new(null, "__ROOT__");

            while (stream.HasNext() && stream.Peek().type != TokenType.EOF)
            {
                if (stream.Peek().type == TokenType.EOF)
                    return rootNode;

                Node scope = ExpectNewScope();
                if (scope != null)
                    rootNode.AddChild(scope);
            }

            totalParseTime += (Program.CurrentTimeMillis() - start);
            return rootNode;
        }

        private Node ExpectNewScope()
        {
            switch (stream.Peek().type)
            {
                case TokenType.CLASS:
                    {
                        return ExpectClass();
                    }

                case TokenType.EOF:
                    {
                        return null;
                    }

                default:
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.EXPECTED_ROOT_MEMBER,
                            message: "Unexpected '" + stream.Peek().value + "', expected class or namespace",
                            file: file,
                            rootCause: stream.Peek()
                        ));
                        stream.Next();
                        return null;
                    }
            }
        }

        private Node ExpectClass()
        {
            stream.Next(); // consume 'class'

            // Class name
            Token nameToken = stream.Next();
            if (nameToken.type != TokenType.IDENTIFIER)
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                    message: "class name must be identifier",
                    file: file,
                    rootCause: nameToken
                ));
                return null;
            }

            // Opening brace
            if (stream.Peek().type == TokenType.BRACE_LEFT)
            {
                stream.Next(); // consume '{'

                ClassTemplateNode templateNode = new(nameToken.value, nameToken);

                while (stream.Peek().type != TokenType.BRACE_RIGHT && stream.Peek().type != TokenType.EOF)
                {
                    templateNode.AddChild(ExpectClassMember());
                }

                if (stream.Peek().type != TokenType.BRACE_RIGHT)
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.EXPECTED_CLASS_MEMBER,
                        message: "expected function or '}', got " + stream.Peek().type,
                        file: file,
                        rootCause: stream.Peek()
                    ));
                } else
                {
                    stream.Next();
                }

                return templateNode;
            } else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_SCOPE,
                    message: "class must have a scope (curly brackets)",
                    file: file,
                    rootCause: stream.Peek()
                ));
                return null;
            }
        }

        private Node ExpectClassMember()
        {
            Token token = stream.Peek();
            if (token.type == TokenType.FUNCTION)
            {
                return ExpectFunction();
            }
            else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_CLASS_MEMBER,
                    message: "expected function, got " + token.type,
                    file: file,
                    rootCause: token
                ));
                stream.Next();
                return null;
            }
        }

        private Node ExpectFunction()
        {
            stream.Next(); // consume 'func'
            Token typeToken = stream.Next();
            if (typeToken.type != TokenType.IDENTIFIER)
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                    message: "function type must be identifier",
                    file: file,
                    rootCause: typeToken
                ));
                return null;
            }

            while (stream.Peek().type == TokenType.DOT)
            {
                stream.Next(); // consume dot
                Token nextToken = stream.Next();
                if (nextToken.type == TokenType.IDENTIFIER)
                {
                    typeToken = new Token(TokenType.IDENTIFIER, typeToken.value + "." + nextToken.value, typeToken.line, typeToken.col);
                } else
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.EXPECTED_IDENTIFIER_IN_TYPE_NAME,
                        message: "expected identifier in nested type name, got " + nextToken.type,
                        file: file,
                        rootCause: nextToken
                    ));
                }
            }

            Token nameToken = stream.Next();
            if (nameToken.type != TokenType.IDENTIFIER)
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                    message: "function name must be identifier, got " + nameToken.type,
                    file: file,
                    rootCause: nameToken
                ));
                return null;
            }

            // Collect arguments
            List<KeyValuePair<NameReferenceNode, NameReferenceNode>> args = new();
            // Opening paren
            if (stream.Peek().type == TokenType.PAREN_LEFT)
            {
                stream.Next(); // consume '('
                bool closedArgs = false;
                while (stream.Peek().type != TokenType.PAREN_RIGHT)
                {
                    if (stream.Peek().type == TokenType.EOF)
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.UNEXPECTED_EOF,
                            message: "unexpected EOF - unfinished argument list",
                            file: file,
                            rootCause: stream.Peek()
                        ));
                        return null;
                    }

                    Token argTypeToken = stream.Next();
                    if (argTypeToken.type != TokenType.IDENTIFIER)
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                            message: "expected identifier for type, got " + argTypeToken.type,
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }
                    Token argNameToken = stream.Next();
                    if (argNameToken.type != TokenType.IDENTIFIER)
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                            message: "expected identifier for name, got " + argNameToken.type,
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }

                    args.Add(new(new NameReferenceNode(argTypeToken.value, argTypeToken), new NameReferenceNode(argNameToken.value, argNameToken)));

                    if (stream.Peek().type == TokenType.COMMA)
                    {
                        if (stream.Peek(2).type == TokenType.PAREN_RIGHT)
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.CANNOT_HAVE_PAREN_AFTER_COMMA_ARGLIST,
                                message: "cannot have ')' after comma in argument list",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }
                        stream.Next();
                    }
                    else if (stream.Peek().type == TokenType.PAREN_RIGHT)
                    {
                        closedArgs = true;
                        stream.Next();
                        break;
                    }
                    else
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.EXPECTED_COMMA_ARGLIST,
                            message: "expected comma",
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }
                }

                if (!closedArgs)
                {
                    if (stream.Peek().type == TokenType.PAREN_RIGHT)
                    {
                        stream.Next();
                    }
                    else
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.EXPECTED_PAREN_CLOSE_ARGLIST,
                            message: "expected ')' to close arguments list, got " + stream.Peek().type,
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }
                }
            }

            // Opening brace
            if (stream.Peek().type == TokenType.BRACE_LEFT)
            {
                stream.Next(); // consume '{'

                FuncNode funcNode = new(nameToken.value, typeToken.value, args, nameToken);

                while (stream.Peek().type != TokenType.BRACE_RIGHT && stream.Peek().type != TokenType.EOF)
                {
                    funcNode.AddChild(ExpectStatement());
                }

                if (stream.Peek().type != TokenType.BRACE_RIGHT)
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.EXPECTED_FUNCTION_MEMBER,
                        message: "expected statement or '}', got " + stream.Peek().type,
                        file: file,
                        rootCause: stream.Peek()
                    ));
                }
                else
                {
                    stream.Next();
                }

                return funcNode;
            }
            else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_SCOPE,
                    message: "function must have a scope (curly brackets)",
                    file: file,
                    rootCause: stream.Peek()
                ));
                return null;
            }
        }

        private Node ExpectStatement()
        {
            // Statements in Marlin:
            // 1. Function calls
            // 2. Variable assignment
            // 3. ;
            switch (stream.Peek().type)
            {
                case TokenType.IDENTIFIER:
                    return ExpectIdentifier(true);
                case TokenType.BRACE_LEFT:
                    return ExpectAnonymousScope();
                case TokenType.SEMICOLON:
                    stream.Next();
                    return null;
                default:
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.EXPECTED_STATEMENT,
                        message: "expected statement, got " + stream.Peek().type,
                        file: file,
                        rootCause: stream.Peek()
                    ));
                    stream.Next();
                    return null;
            }
        }

        private Node ExpectExpression()
        {
            Node n;
            switch (stream.Peek().type)
            {
                case TokenType.IDENTIFIER:
                    n =  ExpectIdentifier(false);
                    break;
                case TokenType.INTEGER:
                    n = ExpectInteger();
                    break;
                case TokenType.DECIMAL:
                    n = ExpectDecimal();
                    break;
                case TokenType.STRING:
                    n = ExpectString();
                    break;
                case TokenType.BOOLEAN:
                    n = ExpectBoolean();
                    break;
                case TokenType.PAREN_LEFT:
                    n = ExpectParenExpression();
                    break;
                case TokenType.SEMICOLON:
                    stream.Next();
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.UNEXPECTED_SEMICOLON,
                        message: "unexpected semicolon",
                        file: file,
                        rootCause: stream.Peek()
                    ));
                    return null;
                default:
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.EXPECTED_EXPRESSION,
                        message: "expected variable, operator or literal, got " + stream.Peek().type,
                        file: file,
                        rootCause: stream.Peek()
                    ));
                    stream.Next();
                    return null;
            }

            if (IsBinaryOperator(stream.Peek().type))
            {
                return HandleOperatorRHS(0, n);
            }
            else
            {
                return n;
            }
        }

        private Node ExpectAnonymousScope()
        {
            // consume '{'
            Node n = new(stream.Next());
            while (stream.Peek().type != TokenType.BRACE_RIGHT && stream.Peek().type != TokenType.EOF)
            {
                n.AddChild(ExpectStatement());
            }

            if (stream.Peek().type == TokenType.BRACE_RIGHT)
            {
                stream.Next();
            } else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_ANON_SCOPE_MEMBER,
                    message: "expected '}', got " + stream.Peek().type,
                    file: file,
                    rootCause: stream.Peek()
                ));
            }

            return n;
        }

        private Node ExpectParenExpression()
        {
            stream.Next(); // consume '('
            Node result = ExpectExpression();
            if (stream.Peek().type != TokenType.PAREN_RIGHT)
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_PAREN_CLOSE_EXPR,
                    message: "expected ')' to close parenthesised expression, got " + stream.Peek().type,
                    file: file,
                    rootCause: stream.Peek()
                ));
            } else
            {
                stream.Next(); // comsume ')'
            }
            return result;
        }

        private Node ExpectIdentifier(bool isStatement)
        {
            Token identifier = stream.Next();
            if (isStatement)
            {
                // Acceptable: function call, variable declaration and assignment

                // Function call
                if (stream.Peek().type == TokenType.PAREN_LEFT)
                {
                    // Collect arguments
                    List<Node> args = new();
                    // Opening paren
                    stream.Next(); // consume '('
                    bool closedArgs = false;
                    while (stream.Peek().type != TokenType.PAREN_RIGHT)
                    {
                        if (stream.Peek().type == TokenType.EOF)
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.UNEXPECTED_EOF,
                                message: "unexpected EOF - unfinished argument list",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                            return null;
                        }

                        args.Add(ExpectExpression());

                        if (stream.Peek().type == TokenType.COMMA)
                        {
                            if (stream.Peek(2).type == TokenType.PAREN_RIGHT)
                            {
                                warnings.Add(new(
                                    level: Level.ERROR,
                                    source: Source.PARSER,
                                    code: ErrorCode.CANNOT_HAVE_PAREN_AFTER_COMMA_ARGLIST,
                                    message: "cannot have ')' after comma in argument list",
                                    file: file,
                                    rootCause: stream.Peek()
                                ));
                            }
                            stream.Next();
                        }
                        else if (stream.Peek().type == TokenType.PAREN_RIGHT)
                        {
                            closedArgs = true;
                            stream.Next();
                            break;
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_COMMA_ARGLIST,
                                message: "expected comma",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }
                    }

                    if (!closedArgs)
                    {
                        if (stream.Peek().type == TokenType.PAREN_RIGHT)
                        {
                            stream.Next();
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_PAREN_CLOSE_ARGLIST,
                                message: "expected ')' to close arguments list, got " + stream.Peek().type,
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }
                    }

                    // We want a semicolon here
                    if (stream.Peek().type == TokenType.SEMICOLON)
                    {
                        stream.Next();
                    }
                    else
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.MISSING_SEMICOLON,
                            message: "missing semicolon",
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }

                    return new FuncCallNode(identifier.value, args, identifier);
                }

                // Variable declaration
                if (stream.Peek().type == TokenType.DOT || stream.Peek().type == TokenType.IDENTIFIER)
                {
                    while (stream.Peek().type == TokenType.DOT)
                    {
                        stream.Next(); // consume dot
                        Token nextToken = stream.Next();
                        if (nextToken.type == TokenType.IDENTIFIER)
                        {
                            identifier = new Token(TokenType.IDENTIFIER, identifier.value + "." + nextToken.value, identifier.line, identifier.col);
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_IDENTIFIER_IN_TYPE_NAME,
                                message: "expected identifier in nested type name, got " + nextToken.type,
                                file: file,
                                rootCause: nextToken
                            ));
                        }
                    }

                    if (stream.Peek().type == TokenType.IDENTIFIER)
                    {
                        Token nameToken = stream.Next();
                    
                        if (stream.Peek().type == TokenType.SET)
                        {
                            stream.Next(); // consume '='
                        }
                        else if (stream.Peek().type == TokenType.SEMICOLON)
                        {
                            stream.Next(); // comesume ';'
                            return new VarDeclareNode(identifier.value, nameToken.value, null, identifier);
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_SET_SEMICOLON_DECLARE_VAR,
                                message: "expected value or semicolon, got " + stream.Peek(),
                                file: file,
                                rootCause: stream.Peek()
                            ));
                            return new VarDeclareNode(identifier.value, nameToken.value, null, identifier);
                        }

                        Node value = ExpectExpression();

                        // We want a semicolon here
                        if (stream.Peek().type == TokenType.SEMICOLON)
                        {
                            stream.Next();
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.MISSING_SEMICOLON,
                                message: "missing semicolon",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }

                        return new VarDeclareNode(identifier.value, nameToken.value, value, identifier);
                    }
                    else
                    {
                        // Got nested type but no identifier?????
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.NAME_MUST_BE_IDENTIFIER,
                            message: "variable name must be identifier, got " + stream.Peek().type,
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }
                }

                // Variable assignment
                if (IsAssignmentOperator(stream.Peek().type))
                {
                    // TODO: Move assignemnt operators than just =
                    _ = stream.Next();
                    Node value = ExpectExpression();

                    // We want a semicolon here
                    if (stream.Peek().type == TokenType.SEMICOLON)
                    {
                        stream.Next();
                    }
                    else
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            code: ErrorCode.MISSING_SEMICOLON,
                            message: "missing semicolon",
                            file: file,
                            rootCause: stream.Peek()
                        ));
                    }

                    return new VarAssignNode(identifier.value, value, identifier);
                }

                // We haven't returned yet! This is not supported
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    code: ErrorCode.EXPECTED_FUNC_CALL_OR_VAR_ASSIGN,
                    message: "expected function call or variable assignment, got " + stream.Peek().type,
                    file: file,
                    rootCause: stream.Peek()
                ));
                return null;
            }
            else
            {
                // Function call
                if (stream.Peek().type == TokenType.PAREN_LEFT)
                {
                    // Collect arguments
                    List<Node> args = new();
                    // Opening paren
                    stream.Next(); // consume '('
                    bool closedArgs = false;
                    while (stream.Peek().type != TokenType.PAREN_RIGHT)
                    {
                        if (stream.Peek().type == TokenType.EOF)
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.UNEXPECTED_EOF,
                                message: "unexpected EOF - unfinished argument list",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                            return null;
                        }

                        args.Add(ExpectExpression());

                        if (stream.Peek().type == TokenType.COMMA)
                        {
                            if (stream.Peek(2).type == TokenType.PAREN_RIGHT)
                            {
                                warnings.Add(new(
                                    level: Level.ERROR,
                                    source: Source.PARSER,
                                    code: ErrorCode.CANNOT_HAVE_PAREN_AFTER_COMMA_ARGLIST,
                                    message: "cannot have ')' after comma in argument list",
                                    file: file,
                                    rootCause: stream.Peek()
                                ));
                            }
                            stream.Next();
                        }
                        else if (stream.Peek().type == TokenType.PAREN_RIGHT)
                        {
                            closedArgs = true;
                            stream.Next();
                            break;
                        } else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_COMMA_ARGLIST,
                                message: "expected comma",
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }
                    }

                    if (!closedArgs)
                    {
                        if (stream.Peek().type == TokenType.PAREN_RIGHT)
                        {
                            stream.Next();
                        }
                        else
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                code: ErrorCode.EXPECTED_PAREN_CLOSE_ARGLIST,
                                message: "expected ')' to close arguments list, got " + stream.Peek().type,
                                file: file,
                                rootCause: stream.Peek()
                            ));
                        }
                    }

                    return new FuncCallNode(identifier.value, args, identifier);
                }

                // Variable reference
                return new NameReferenceNode(identifier.value, identifier);
            }
        }

        private Node ExpectInteger()
        {
            Token integer = stream.Next();
            if (integer.type == TokenType.INTEGER)
            {
                return new NumberIntegerNode(int.Parse(integer.value), integer);
            } else
            {
                throw new Exception("Can't create int");
            }
        }

        private Node ExpectDecimal()
        {
            Token dbl = stream.Next();
            if (dbl.type == TokenType.DECIMAL)
            {
                return new NumberDoubleNode(double.Parse(dbl.value, CultureInfo.InvariantCulture), dbl);
            } else
            {
                throw new Exception("Can't create double");
            }
        }

        private Node ExpectString()
        {
            Token str = stream.Next();
            if (str.type == TokenType.STRING)
            {
                return new StringNode(str.value, str);
            }
            else
            {
                throw new Exception("Can't create string");
            }
        }

        private Node ExpectBoolean()
        {
            Token bln = stream.Next();
            if (bln.type == TokenType.BOOLEAN)
            {
                return new BooleanNode(bln.value == "true", bln);
            }
            else
            {
                throw new Exception("Can't create string");
            }
        }

        private Node HandleOperatorRHS(int expressionPrecedence, Node left)
        {
            while (true)
            {
                int tokenPrecedence = GetTokenPrecedence(stream.Peek().type);

                // If this is a binop that binds at least as tightly as the current binop,
                // consume it, otherwise we are done.
                if (tokenPrecedence < expressionPrecedence)
                {
                    return left;
                }

                // Okay, we know this is a binop
                Token binOp = stream.Next();

                Node right = ExpectExpression();
                if (right == null)
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        code: ErrorCode.MISSING_OPERATOR_RIGHT,
                        message: "expected something to the right of '" + binOp.value + "'",
                        file: file,
                        rootCause: stream.Peek()
                    ));
                    return null;
                }

                // If BinOp binds less tightly with RHS than the operator after RHS, let
                // the pending operator take RHS as its LHS.
                int nextPrecedence = GetTokenPrecedence(stream.Peek().type);
                if (tokenPrecedence < nextPrecedence)
                {
                    right = HandleOperatorRHS(tokenPrecedence + 1, right);
                    if (right == null)
                    {
                        return null;
                    }
                }

                left = new BinaryOperatorNode(binOp.value, left, right, binOp);
            }
        }
    }
}
