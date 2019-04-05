using SpeedCalc.Numeric;

using Xunit;

namespace SpeedCalc.CoreTests.Numeric
{
    public class BigIntEquality
    {
        [Fact]
        public void CompareZeroConstant()
        {
            var value = new BigInteger(0);
            Assert.Equal(BigInteger.Zero, value);
        }

        [Fact]
        public void CompareOneConstant()
        {
            var value = new BigInteger(1);
            Assert.Equal(BigInteger.One, value);
        }

        [Fact]
        public void CompareMinusOneConstant()
        {
            var value = new BigInteger(-1);
            Assert.Equal(BigInteger.MinusOne, value);
        }
    }
}
