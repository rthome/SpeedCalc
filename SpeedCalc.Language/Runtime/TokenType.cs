namespace SpeedCalc.Core.Runtime
{
    public enum TokenType
    {
        Error,
        EOF,

        // Single-character
        BraceLeft,
        BraceRight,
        Comma,
        Dot,
        Minus,
        ParenLeft,
        ParenRight,
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
        For,
        If,
        Or,
        Return,
        True,
        Var,
        While
    }
}
