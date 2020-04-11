namespace SpeedCalc.Core.Runtime
{
    public enum TokenType
    {
        Error,
        EOF,

        // Single-character
        ParenLeft,
        ParenRight,
        BraceLeft,
        BraceRight,
        Comma,
        Dot,
        Colon,
        Semicolon,
        Minus,
        Plus,
        Slash,

        // One/two character
        Bang,
        BangEqual,
        Equal,
        EqualEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        Star,
        StarStar,

        // Literals
        Identifier,
        Number,

        // Keywords
        And,
        Else,
        False,
        Fn,
        For,
        If,
        Or,
        Print,
        Return,
        True,
        Var,
        While,
    }
}
