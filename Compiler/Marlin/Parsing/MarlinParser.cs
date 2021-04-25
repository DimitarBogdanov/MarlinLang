using Marlin.Lexing;

namespace Marlin.Parsing
{
    public class MarlinParser
    {
        private readonly TokenStream tokenStream;

        public MarlinParser(TokenStream stream)
        {
            tokenStream = stream;
        }

        public void Parse()
        {
            tokenStream.Next();
        }
    }
}
