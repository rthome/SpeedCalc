using System;

namespace SpeedCalc.Numeric
{
    /// <summary>
    /// Implements a signed integer of arbitrary size (only limited by available memory)
    /// </summary>
    public readonly struct BigInteger : IComparable<BigInteger>, IEquatable<BigInteger>
    {
        readonly int sign;
        readonly Memory<uint> bits;

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

        public override string ToString()
        {
            return base.ToString();
        }

        // TODO: Add overloads with number styles, format providers, etc

        #endregion

        #region Constructors

        public BigInteger(int value)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
