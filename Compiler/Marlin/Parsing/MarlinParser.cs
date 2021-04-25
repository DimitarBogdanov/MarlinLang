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

        public Node Parse()
        {
            Node rootNode = new Node("__ROOT__");

            

            return rootNode;
        }
    }
}
