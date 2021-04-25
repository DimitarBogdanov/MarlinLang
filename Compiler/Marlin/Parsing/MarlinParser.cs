using Marlin.Lexing;
using System;
using System.Collections.Generic;
using static Marlin.CompilerWarning;

namespace Marlin.Parsing
{
    public class MarlinParser
    {
        private readonly TokenStream stream;
        private bool encounteredErrors = false;
        public List<CompilerWarning> warnings = new();

        public MarlinParser(TokenStream stream)
        {
            this.stream = stream;
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
                            level:     Level.ERROR,
                            message:   "Unexpected '" + stream.Peek().value + "', expected class or namespace",
                            rootCause: stream.Peek()
                        ));
                        stream.Next();
                        return null;
                    }
            }
        }

        #region Scopes and their members
        private Node ExpectClass()
        {
            stream.Next(); // consume 'class'

            // Class name
            Token nameToken = stream.Next();
            if (nameToken.type != TokenType.IDENTIFIER)
            {
                warnings.Add(new(
                    level: Level.ERROR,
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
                return null;//ExpectFunction();
            }
            else
            {
                warnings.Add(new(
                    level: Level.ERROR,
                    message: "expected function, got " + token.type,
                    rootCause: token
                ));
                return null;
            }
        }
        #endregion Scopes
    }
}
