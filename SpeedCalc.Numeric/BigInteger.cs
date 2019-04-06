using System;
using System.Globalization;

namespace SpeedCalc.Numeric
{
    /// <summary>
    /// Implements a signed integer of arbitrary size (only limited by available memory)
    /// </summary>
    public readonly struct BigInteger : IComparable<BigInteger>, IEquatable<BigInteger>
    {
        readonly int sign;
        readonly ReadOnlyMemory<uint> bits;

        #region Static Properties

        public static BigInteger Zero { get; } = new BigInteger(0);

        public static BigInteger One { get; } = new BigInteger(1);

        public static BigInteger MinusOne { get; } = new BigInteger(-1);

        #endregion

        #region Static Arithmetic Methods

        public static ref readonly BigInteger Add(in BigInteger left, in BigInteger right)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Static Parse Methods

        /// <summary>
        /// Parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <returns>A new big integer</returns>
        public static BigInteger Parse(string value) => Parse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo);

        /// <summary>
        /// Parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="style">Number styles to permit</param>
        /// <returns>A new big integer</returns>
        public static BigInteger Parse(string value, NumberStyles style) => Parse(value, style, NumberFormatInfo.CurrentInfo);

        /// <summary>
        /// Parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="provider">Number format provider to use for culture-dependent formatting</param>
        /// <returns>A new big integer</returns>
        public static BigInteger Parse(string value, IFormatProvider provider) => Parse(value, NumberStyles.Integer, provider);

        /// <summary>
        /// Parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="style">Number styles to permit</param>
        /// <param name="provider">Number format provider to use for culture-dependent formatting</param>
        /// <returns>A new big integer</returns>
        public static BigInteger Parse(string value, NumberStyles style, IFormatProvider provider)
        {
            if (TryParse(value, style, provider, out var result))
                return result;
            else
                throw new FormatException("Unable to parse big integer from given string");
        }

        /// <summary>
        /// Try to parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="result">If successful, contains the parsed big integer</param>
        /// <returns>True if a big integer could be parsed; otherwise false</returns>
        public static bool TryParse(string value, out BigInteger result) => TryParse(value, NumberStyles.Integer, NumberFormatInfo.CurrentInfo, out result);

        /// <summary>
        /// Try to parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="style">Number styles to permit</param>
        /// <param name="result">If successful, contains the parsed big integer</param>
        /// <returns>True if a big integer could be parsed; otherwise false</returns>
        public static bool TryParse(string value, NumberStyles style, out BigInteger result) => TryParse(value, style, NumberFormatInfo.CurrentInfo, out result);

        /// <summary>
        /// Try to parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="provider">Number format provider to use for culture-dependent formatting</param>
        /// <param name="result">If successful, contains the parsed big integer</param>
        /// <returns>True if a big integer could be parsed; otherwise false</returns>
        public static bool TryParse(string value, IFormatProvider provider, out BigInteger result) => TryParse(value, NumberStyles.Integer, provider, out result);

        /// <summary>
        /// Try to parse a big integer from a string
        /// </summary>
        /// <param name="value">The string to parse</param>
        /// <param name="style">Number styles to permit</param>
        /// <param name="provider">Number format provider to use for culture-dependent formatting</param>
        /// <param name="result">If successful, contains the parsed big integer</param>
        /// <returns>True if a big integer could be parsed; otherwise false</returns>
        public static bool TryParse(string value, NumberStyles style, IFormatProvider provider, out BigInteger result) => BigIntegerUtils.TryParse(value, style, NumberFormatInfo.GetInstance(provider), out result);

        #endregion

        #region Operator Overloads

        public static BigInteger operator +(in BigInteger left, in BigInteger right) => Add(left, right);

        #endregion

        #region Equality & Comparison

        public int CompareTo(BigInteger other)
        {
            throw new NotImplementedException();
        }

        public bool Equals(BigInteger other)
        {
            throw new NotImplementedException();
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        #endregion

        #region ToString

        /// <summary>
        /// Format the big integer as a string
        /// </summary>
        /// <returns>A string representation of the big integer</returns>
        public override string ToString() => ToString(null, NumberFormatInfo.CurrentInfo);

        /// <summary>
        /// Format the big integer as a string
        /// </summary>
        /// <param name="provider">The format provider to use</param>
        /// <returns>A string representation of the big integer</returns>
        public string ToString(IFormatProvider provider) => ToString(null, provider);

        /// <summary>
        /// Format the big integer as a string
        /// </summary>
        /// <param name="format">The format string to use</param>
        /// <returns>A string representation of the big integer</returns>
        public string ToString(string format) => ToString(format, NumberFormatInfo.CurrentInfo);

        /// <summary>
        /// Format the big integer as a string
        /// </summary>
        /// <param name="format">The format string to use</param>
        /// <param name="provider">The format provider to use</param>
        /// <returns>A string representation of the big integer</returns>
        public string ToString(string format, IFormatProvider provider) => BigIntegerUtils.ToString(this, format, NumberFormatInfo.GetInstance(provider));

        #endregion

        #region Constructors

        public BigInteger(int value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
