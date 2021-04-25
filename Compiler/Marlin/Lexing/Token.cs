namespace Marlin.Lexing
{
    public class Token
    {
        public readonly TokenType type;
        public readonly string value;
        public readonly int line;
        public readonly int col;

        public Token(TokenType type, string value, int line, int col)
        {
            this.type = type;
            this.value = value;
            this.line = line;
            this.col = col;
        }

        public override string ToString()
        {
            return $"Token({type},\"{value}\",{line}:{col})";
        }
    }
}
