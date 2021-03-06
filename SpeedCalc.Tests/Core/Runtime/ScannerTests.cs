using SpeedCalc.Core.Runtime;

using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using Xunit;

using static SpeedCalc.Core.Runtime.TokenType;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ScannerTests
    {
        public static IEnumerable<object[]> Digits => Enumerable.Range(0, 9).Select(i => new object[] { i.ToString(CultureInfo.InvariantCulture) });

        public static IEnumerable<object[]> ScanSamples
        {
            get
            {
                yield return new object[] { "1+1", new[] { (Number, "1"), (Plus, "+"), (Number, "1") } };
                yield return new object[] { "f(x) = x**2;",
                    new[] { (Identifier, "f"), (ParenLeft, "("), (Identifier, "x"), (ParenRight, ")"), (Equal, "="), (Identifier, "x"), (StarStar, "**"), (Number, "2"), (Semicolon, ";") } };
                yield return new object[] { "sin(alpha) = cos(deg(90) - alpha);",
                    new[] { (Identifier, "sin"), (ParenLeft, "("), (Identifier, "alpha"), (ParenRight, ")"), (Equal, "="),
                        (Identifier, "cos"), (ParenLeft, "("), (Identifier, "deg"), (ParenLeft, "("), (Number, "90"), (ParenRight, ")"), (Minus, "-"), (Identifier, "alpha"), (ParenRight, ")"), (Semicolon, ";") } };
                yield return new object[] { "fn one { return 1; }", new[] { (Fn, "fn"), (Identifier, "one"), (BraceLeft, "{"), (Return, "return"), (Number, "1"), (Semicolon, ";"), (BraceRight, "}") } };
            }
        }

        static void ScanSingleToken(string lexeme, TokenType expectedType)
        {
            IEnumerable<string> WhitespaceLexeme()
            {
                yield return lexeme;
                yield return $" {lexeme}";
                yield return $"{lexeme} ";
                yield return $"  {lexeme}";
                yield return $"\t{lexeme}";
                yield return $"  \t{lexeme}";
                yield return $"  \t \t {lexeme}   ";
                yield return $"\r \t \n {lexeme}";
                yield return $"\r\n\t{lexeme}";
            }

            foreach (var whitespacedLexeme in WhitespaceLexeme())
            {
                var scanner = new Scanner(whitespacedLexeme);
                var token = scanner.ScanToken();

                Assert.Equal(expectedType, token.Type);
                Assert.Equal(lexeme, token.Lexeme);
            }
        }

        [Fact]
        public void Eof()
        {
            ScanSingleToken(string.Empty, EOF);
        }

        [Theory]
        [InlineData("{", BraceLeft)]
        [InlineData("}", BraceRight)]
        [InlineData(",", Comma)]
        [InlineData(".", Dot)]
        [InlineData(":", Colon)]
        [InlineData(";", Semicolon)]
        [InlineData("(", ParenLeft)]
        [InlineData(")", ParenRight)]
        [InlineData("!", Bang)]
        [InlineData("!=", BangEqual)]
        [InlineData("=", Equal)]
        [InlineData("==", EqualEqual)]
        [InlineData(">", Greater)]
        [InlineData(">=", GreaterEqual)]
        [InlineData("<", Less)]
        [InlineData("<=", LessEqual)]
        [InlineData("-", Minus)]
        [InlineData("-=", MinusEqual)]
        [InlineData("+", Plus)]
        [InlineData("+=", PlusEqual)]
        [InlineData("/", Slash)]
        [InlineData("/=", SlashEqual)]
        [InlineData("*", Star)]
        [InlineData("*=", StarEqual)]
        [InlineData("**", StarStar)]
        [InlineData("**=", StarStarEqual)]
        public void SimpleTokens(string lexeme, TokenType expectedType)
        {
            ScanSingleToken(lexeme, expectedType);
        }

        [Theory]
        [InlineData("and", And)]
        [InlineData("else", Else)]
        [InlineData("false", False)]
        [InlineData("fn", Fn)]
        [InlineData("for", For)]
        [InlineData("if", If)]
        [InlineData("or", Or)]
        [InlineData("return", Return)]
        [InlineData("true", True)]
        [InlineData("var", Var)]
        [InlineData("while", While)]
        [InlineData("print", Print)]
        public void KeywordTokens(string lexeme, TokenType expectedType)
        {
            ScanSingleToken(lexeme, expectedType);
        }

        [Theory]
        [InlineData("x")]
        [InlineData("X")]
        [InlineData("x0")]
        [InlineData("variable")]
        [InlineData("trueValue")]
        [InlineData("return0")]
        [InlineData("x1y")]
        [InlineData("asd123098lkj")]
        public void IdentifierTokens(string lexeme)
        {
            ScanSingleToken(lexeme, Identifier);
        }

        [Theory]
        [MemberData(nameof(Digits))]
        [InlineData("100000000000")]
        [InlineData("00000000")]
        [InlineData("98374928374")]
        [InlineData("123456789123456789123456789")]
        [InlineData("0.0")]
        [InlineData("0.123")]
        [InlineData("123.123")]
        [InlineData("123456789123456789.123456789123456789")]
        [InlineData(".0")]
        [InlineData(".6")]
        [InlineData(".45")]
        [InlineData(".1234")]
        [InlineData(".83749218374982734")]
        public void NumberTokens(string lexeme)
        {
            ScanSingleToken(lexeme, Number);
        }

        [Theory]
        [MemberData(nameof(ScanSamples))]
        public void LongScans(string source, (TokenType type, string lexeme)[] expectedTokens)
        {
            var scanner = new Scanner(source);
            for (int i = 0; i < expectedTokens.Length; i++)
            {
                var token = scanner.ScanToken();
                Assert.Equal(expectedTokens[i].type, token.Type);
                Assert.Equal(expectedTokens[i].lexeme, token.Lexeme);
            }

            Assert.Equal(EOF, scanner.ScanToken().Type);
        }
    }
}
