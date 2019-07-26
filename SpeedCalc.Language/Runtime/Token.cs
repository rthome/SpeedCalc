namespace SpeedCalc.Core.Runtime
{
    public struct Token
    {
        public TokenType Type { get; set; }

        public string Lexeme { get; set; }

        public int Line { get; set; }
    }
}
