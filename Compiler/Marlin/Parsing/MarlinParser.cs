using Marlin.Lexing;
using System;
using System.Collections.Generic;
using static Marlin.CompilerWarning;

namespace Marlin.Parsing
{
    public class MarlinParser
    {
        private readonly TokenStream stream;
        public List<CompilerWarning> warnings = new();

        public MarlinParser(TokenStream stream)
        {
            this.stream = stream;
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
            Node rootNode = new("__ROOT__");

            while (stream.HasNext())
            {
                try
                {
                    if (stream.Peek().type == TokenType.EOF)
                        return rootNode;

                    Node scope = ExpectNewScope();
                    if (scope != null)
                        rootNode.AddChild(scope);
                }
                catch (Exception)
                {
                    // File ended
                    return rootNode;
                }
            }

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
                            message: "Unexpected '" + stream.Peek().value + "', expected class or namespace",
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
                    message: "class name must be identifier",
                    rootCause: nameToken
                ));
                return null;
            }

            // Opening brace
            if (stream.Peek().type == TokenType.BRACE_LEFT)
            {
                stream.Next(); // consume '{'

                ClassTemplateNode templateNode = new(nameToken.value);

                while (stream.Peek().type != TokenType.BRACE_RIGHT && stream.Peek().type != TokenType.EOF)
                {
                    templateNode.AddChild(ExpectClassMember());
                }

                if (stream.Peek().type != TokenType.BRACE_RIGHT)
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        message: "expected function or '}', got " + stream.Peek().type,
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
                    message: "class must have a scope (curly brackets)",
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
                    message: "expected function, got " + token.type,
                    rootCause: token
                ));
                stream.Next();
                return null;
            }
        }

        private Node ExpectFunction()
        {
            stream.Next(); // consume 'func'
            Token nameToken = stream.Next();
            if (nameToken.type != TokenType.IDENTIFIER)
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    message: "function name must be identifier",
                    rootCause: nameToken
                ));
                return null;
            }

            // Collect arguments
            List<KeyValuePair<VarNode, VarNode>> args = new();
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
                            message: "unexpected EOF - unfinished argument list",
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
                            message: "expected identifier for type, got " + argTypeToken.type,
                            rootCause: stream.Peek()
                        ));
                    }
                    Token argNameToken = stream.Next();
                    if (argNameToken.type != TokenType.IDENTIFIER)
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            message: "expected identifier for name, got " + argNameToken.type,
                            rootCause: stream.Peek()
                        ));
                    }

                    args.Add(new(new VarNode(argTypeToken.value), new VarNode(argNameToken.value)));

                    if (stream.Peek().type == TokenType.COMMA)
                    {
                        if (stream.Peek(2).type == TokenType.PAREN_RIGHT)
                        {
                            warnings.Add(new(
                                level: Level.ERROR,
                                source: Source.PARSER,
                                message: "cannot have ')' after comma in argument list",
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
                            message: "expected comma",
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
                            message: "expected ')' to close arguments list, got " + stream.Peek().type,
                            rootCause: stream.Peek()
                        ));
                    }
                }
            }

            // Opening brace
            if (stream.Peek().type == TokenType.BRACE_LEFT)
            {
                stream.Next(); // consume '{'

                FuncNode funcNode = new(nameToken.value, args);

                while (stream.Peek().type != TokenType.BRACE_RIGHT && stream.Peek().type != TokenType.EOF)
                {
                    funcNode.AddChild(ExpectStatement());
                }

                if (stream.Peek().type != TokenType.BRACE_RIGHT)
                {
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        message: "expected statement or '}', got " + stream.Peek().type,
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
                    message: "function must have a scope (curly brackets)",
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
                        message: "expected statement, got " + stream.Peek().type,
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
                case TokenType.SEMICOLON:
                    stream.Next();
                    return null;
                default:
                    warnings.Add(new(
                        level: Level.ERROR,
                        source: Source.PARSER,
                        message: "expected variable or number, got " + stream.Peek().type,
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
            stream.Next(); // consume '{'
            Node n = new();
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
                    message: "expected '}', got " + stream.Peek().type,
                    rootCause: stream.Peek()
                ));
            }

            return n;
        }

        private Node ExpectIdentifier(bool isStatement)
        {
            Token identifier = stream.Next();
            if (isStatement)
            {
                // Acceptable: function call, variable assignment

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
                                message: "unexpected EOF - unfinished argument list",
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
                                    message: "cannot have ')' after comma in argument list",
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
                                message: "expected comma",
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
                                message: "expected ')' to close arguments list, got " + stream.Peek().type,
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
                            message: "missing semicolon",
                            rootCause: stream.Peek()
                        ));
                    }

                    return new FuncCallNode(identifier.value, args);
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
                            message: "missing semicolon",
                            rootCause: stream.Peek()
                        ));
                    }

                    return new VarAssignNode(identifier.value, value);
                }

                // We haven't returned yet! This is not supported
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    message: "expected function call or variable assignment, got " + stream.Peek().type,
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
                                message: "unexpected EOF - unfinished argument list",
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
                                    message: "cannot have ')' after comma in argument list",
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
                                message: "expected comma",
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
                                message: "expected ')' to close arguments list, got " + stream.Peek().type,
                                rootCause: stream.Peek()
                            ));
                        }
                    }

                    // We DON'T want a semicolon here
                    if (stream.Peek().type == TokenType.SEMICOLON)
                    {
                        warnings.Add(new(
                            level: Level.ERROR,
                            source: Source.PARSER,
                            message: "unexpected semicolon",
                            rootCause: stream.Peek()
                        ));
                        stream.Next();
                    }

                    return new FuncCallNode(identifier.value, args);
                }

                // Variable reference
                return new VarNode(identifier.value);
            }
        }

        private Node ExpectInteger()
        {
            Token integer = stream.Next();
            if (integer.type == TokenType.INTEGER)
            {
                return new NumberIntegerNode(int.Parse(integer.value));
            } else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    message: "expected integer, got " + stream.Peek().type,
                    rootCause: stream.Peek()
                ));
                return null;
            }
        }

        private Node ExpectDecimal()
        {
            Token dbl = stream.Next();
            if (dbl.type == TokenType.DECIMAL)
            {
                return new NumberDoubleNode(double.Parse(dbl.value));
            } else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    source: Source.PARSER,
                    message: "expected double, got " + stream.Peek().type,
                    rootCause: stream.Peek()
                ));
                return null;
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
                        message: "expected something to the right of '" + binOp.value + "'",
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

                left = new BinaryOperatorNode(binOp.value, left, right);
            }
        }
    }
}
