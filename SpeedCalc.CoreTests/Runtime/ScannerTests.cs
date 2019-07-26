using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using SpeedCalc.Core.Runtime;

using Xunit;

namespace SpeedCalc.CoreTests.Runtime
{
    public class ScannerTests
    {
        public static IEnumerable<object[]> Digits => Enumerable.Range(0, 9).Select(i => new object[] { i.ToString(CultureInfo.InvariantCulture) });

        void ScanSingleToken(string lexeme, TokenType expectedType)
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
            ScanSingleToken(string.Empty, TokenType.EOF);
        }

        [Theory]
        [InlineData("{", TokenType.BraceLeft)]
        [InlineData("}", TokenType.BraceRight)]
        [InlineData(",", TokenType.Comma)]
        [InlineData(".", TokenType.Dot)]
        [InlineData("(", TokenType.ParenLeft)]
        [InlineData(")", TokenType.ParenRight)]
        [InlineData("+", TokenType.Plus)]
        [InlineData("/", TokenType.Slash)]
        [InlineData("!", TokenType.Bang)]
        [InlineData("!=", TokenType.BangEqual)]
        [InlineData("=", TokenType.Equal)]
        [InlineData("==", TokenType.EqualEqual)]
        [InlineData(">", TokenType.Greater)]
        [InlineData(">=", TokenType.GreaterEqual)]
        [InlineData("<", TokenType.Less)]
        [InlineData("<=", TokenType.LessEqual)]
        [InlineData("*", TokenType.Star)]
        [InlineData("**", TokenType.StarStar)]
        public void SimpleTokens(string lexeme, TokenType expectedType)
        {
            ScanSingleToken(lexeme, expectedType);
        }

        [Theory]
        [InlineData("and", TokenType.And)]
        [InlineData("else", TokenType.Else)]
        [InlineData("false", TokenType.False)]
        [InlineData("for", TokenType.For)]
        [InlineData("if", TokenType.If)]
        [InlineData("or", TokenType.Or)]
        [InlineData("return", TokenType.Return)]
        [InlineData("true", TokenType.True)]
        [InlineData("var", TokenType.Var)]
        [InlineData("while", TokenType.While)]
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
            ScanSingleToken(lexeme, TokenType.Identifier);
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
            ScanSingleToken(lexeme, TokenType.Number);
        }
    }
}
