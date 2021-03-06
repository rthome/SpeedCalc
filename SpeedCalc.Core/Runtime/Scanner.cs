﻿namespace SpeedCalc.Core.Runtime
{
    public class Scanner
    {
        readonly string source;
        int start;
        int current;
        int line = 1;

        bool IsAtEnd => current >= source.Length;

        char Peek => IsAtEnd ? default : source[current];

        char PeekNext => (current + 1 >= source.Length) ? default : source[current + 1];

        char Advance() => source[current++];

        bool Match(char expected)
        {
            if (Peek != expected)
                return false;

            current++;
            return true;
        }

        bool IsDigit(char c) => c >= '0' && c <= '9';

        bool IsAlpha(char c) => (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z');

        Token MakeToken(TokenType type) => new Token(type, source[start..current], line);

        void Whitespace()
        {
            while (true)
            {
                switch (Peek)
                {
                    case ' ':
                    case '\r':
                    case '\t':
                        Advance();
                        break;

                    case '\n':
                        Advance();
                        line++;
                        break;

                    case '/':
                        if (PeekNext == '/')
                        {
                            while (Peek != '\n' && !IsAtEnd)
                                Advance();
                        }
                        else
                            return;
                        break;

                    default:
                        return;
                }
            }
        }

        TokenType IdentifierType()
        {
            TokenType CheckKeyword(int offset, string rest, TokenType type)
            {
                if (current - start == offset + rest.Length && source.Substring(start + offset, rest.Length) == rest)
                    return type;
                return TokenType.Identifier;
            }

            switch (source[start])
            {
                case 'a': return CheckKeyword(1, "nd", TokenType.And);
                case 'b': return CheckKeyword(1, "reak", TokenType.Break);
                case 'c': return CheckKeyword(1, "ontinue", TokenType.Continue);
                case 'e': return CheckKeyword(1, "lse", TokenType.Else);
                case 'f':
                    if (current - start > 1)
                    {
                        switch (source[start + 1])
                        {
                            case 'a': return CheckKeyword(2, "lse", TokenType.False);
                            case 'n': return CheckKeyword(2, "", TokenType.Fn);
                            case 'o': return CheckKeyword(2, "r", TokenType.For);
                        }
                    }
                    break;
                case 'i': return CheckKeyword(1, "f", TokenType.If);
                case 'm': return CheckKeyword(1, "od", TokenType.Mod);
                case 'o': return CheckKeyword(1, "r", TokenType.Or);
                case 'p': return CheckKeyword(1, "rint", TokenType.Print);
                case 'r': return CheckKeyword(1, "eturn", TokenType.Return);
                case 't': return CheckKeyword(1, "rue", TokenType.True);
                case 'v': return CheckKeyword(1, "ar", TokenType.Var);
                case 'w': return CheckKeyword(1, "hile", TokenType.While);
            }

            return TokenType.Identifier;
        }

        Token Identifier()
        {
            while (IsAlpha(Peek) || IsDigit(Peek))
                Advance();
            return MakeToken(IdentifierType());
        }

        Token Number()
        {
            while (IsDigit(Peek))
                Advance();

            if (Peek == '.' && IsDigit(PeekNext))
            {
                Advance();
                while (IsDigit(Peek))
                    Advance();
            }

            return MakeToken(TokenType.Number);
        }

        public Token ScanToken()
        {
            Whitespace();

            start = current;

            if (IsAtEnd)
                return MakeToken(TokenType.EOF);

            var c = Advance();

            if (IsDigit(c))
                return Number();
            if (IsAlpha(c))
                return Identifier();

            switch (c)
            {
                case '(': return MakeToken(TokenType.ParenLeft);
                case ')': return MakeToken(TokenType.ParenRight);
                case '{': return MakeToken(TokenType.BraceLeft);
                case '}': return MakeToken(TokenType.BraceRight);
                case ',': return MakeToken(TokenType.Comma);
                case '.':
                    if (IsDigit(Peek))
                        return Number();
                    else
                        return MakeToken(TokenType.Dot);
                case ':': return MakeToken(TokenType.Colon);
                case ';': return MakeToken(TokenType.Semicolon);

                case '-':
                    return MakeToken(Match('=') ? TokenType.MinusEqual : TokenType.Minus);
                case '+':
                    return MakeToken(Match('=') ? TokenType.PlusEqual : TokenType.Plus);
                case '/':
                    return MakeToken(Match('=') ? TokenType.SlashEqual : TokenType.Slash);
                case '*':
                    if (Match('*'))
                        return MakeToken(Match('=') ? TokenType.StarStarEqual : TokenType.StarStar);
                    else
                        return MakeToken(Match('=') ? TokenType.StarEqual : TokenType.Star);
                case '!':
                    return MakeToken(Match('=') ? TokenType.BangEqual : TokenType.Bang);
                case '=':
                    return MakeToken(Match('=') ? TokenType.EqualEqual : TokenType.Equal);
                case '<':
                    return MakeToken(Match('=') ? TokenType.LessEqual : TokenType.Less);
                case '>':
                    return MakeToken(Match('=') ? TokenType.GreaterEqual : TokenType.Greater);
            }

            return new Token(TokenType.Error, "Unexpected character", line);
        }

        public Scanner(string source) => this.source = source ?? throw new System.ArgumentNullException(nameof(source));
    }
}
