using System;
using System.Collections.Generic;

namespace Marlin.Lexing
{
    public class TokenStream
    {
        private readonly List<Token> tokens = new();
        private int pos = -1;

        public Token this[int index]
        {
            get
            {
                return tokens[index];
            }
        }

        public void Add(Token token)
        {
            tokens.Add(token);
        }

        public Token Next()
        {
            if (HasNext())
            {
                pos++;
                return tokens[pos];
            } else
            {
                return null;
            }
        }

        public Token Peek(int with = 1)
        {
            try
            {
                return tokens[pos + with];
            } catch (IndexOutOfRangeException)
            {
                return null;
            }
        }

        public List<Token> GetTokens()
        {
            return tokens;
        }

        public Token GetCurrentToken()
        {
            return tokens[pos];
        }

        public bool HasNext()
        {
            return (pos + 1 < tokens.Count);
        }

        public void RemoveSemicolons()
        {
            tokens.RemoveAll(x => x.type == TokenType.SEMICOLON);
        }
    }
}
