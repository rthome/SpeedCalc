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
        
        // One/two/three character
        Bang,
        BangEqual,
        Equal,
        EqualEqual,
        Greater,
        GreaterEqual,
        Less,
        LessEqual,
        Minus,
        MinusEqual,
        Plus,
        PlusEqual,
        Slash,
        SlashEqual,
        Star,
        StarEqual,
        StarStar,
        StarStarEqual,

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
        Mod,
        Or,
        Print,
        Return,
        True,
        Var,
        While,
    }
}
