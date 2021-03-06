﻿using System;
using System.Globalization;

namespace SpeedCalc.Core.Runtime
{
    public abstract class Value : IEquatable<Value>
    {
        public override sealed string ToString() => Values.ToString(this);

        public override sealed bool Equals(object obj)
        {
            if (obj is null)
                return false;

            if (obj is Value other)
                return Equals(other);
            else
                return false;
        }

        public bool Equals(Value other)
        {
            if (other is null)
                return false;
            return this.EqualsValue(other);
        }

        public abstract override int GetHashCode();
    }

    public static class Values
    {
        #region Value Subtypes

        sealed class BoolVal : Value
        {
            public bool Value { get; }

            public override int GetHashCode() => Value.GetHashCode();

            public BoolVal(bool value) => Value = value;
        }

        sealed class NumberVal : Value
        {
            public decimal Value { get; }

            public override int GetHashCode() => Value.GetHashCode();

            public NumberVal(decimal value) => Value = value;
        }

        sealed class StringVal : Value
        {
            public string Value { get; }

            public override int GetHashCode() => Value.GetHashCode();

            public StringVal(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public sealed class FunctionVal : Value
        {
            public Function Value { get; }

            public override int GetHashCode() => new { Value.Name, Value.Arity }.GetHashCode();

            public FunctionVal(Function value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        static readonly Value TrueInstance = new BoolVal(true);
        static readonly Value FalseInstance = new BoolVal(false);

        public static Value Bool(bool value) => value ? TrueInstance : FalseInstance;

        public static Value Number(decimal value) => new NumberVal(value);

        public static Value String(string value) => new StringVal(value);

        public static Value Function(Function value) => new FunctionVal(value);

        public static bool IsBool(this Value value) => (value ?? throw new ArgumentNullException(nameof(value))) is BoolVal;

        public static bool IsNumber(this Value value) => (value ?? throw new ArgumentNullException(nameof(value))) is NumberVal;

        public static bool IsString(this Value value) => (value ?? throw new ArgumentNullException(nameof(value))) is StringVal;

        public static bool IsFunction(this Value value) => (value ?? throw new ArgumentNullException(nameof(value))) is FunctionVal;

        public static bool AsBool(this Value value)
        {
            if ((value ?? throw new ArgumentNullException(nameof(value))) is BoolVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a bool value.");
        }

        public static decimal AsNumber(this Value value)
        {
            if ((value ?? throw new ArgumentNullException(nameof(value))) is NumberVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a number value.");
        }

        public static string AsString(this Value value)
        {
            if ((value ?? throw new ArgumentNullException(nameof(value))) is StringVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a string value.");
        }

        public static Function AsFunction(this Value value)
        {
            if ((value ?? throw new ArgumentNullException(nameof(value))) is FunctionVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a function value.");
        }

        public static bool EqualsValue(this Value firstValue, Value secondValue)
        {
            if (firstValue.GetType() != secondValue.GetType())
                return false;

            return firstValue switch
            {
                BoolVal _ => firstValue.AsBool() == secondValue.AsBool(),
                NumberVal _ => firstValue.AsNumber() == secondValue.AsNumber(),
                StringVal _ => firstValue.AsString() == secondValue.AsString(),
                FunctionVal _ => ReferenceEquals(firstValue.AsFunction(), secondValue.AsFunction()),
                _ => throw new RuntimeException($"Unknown value types received: '{firstValue.GetType()}' and {secondValue.GetType()}'"),
            };
        }

        public static string ToString(this Value value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return value switch
            {
                BoolVal _ => value.AsBool() ? "true" : "false",
                NumberVal _ => value.AsNumber().ToString("G", CultureInfo.InvariantCulture),
                StringVal val => val.Value,
                FunctionVal val => val.AsFunction().ToString(),
                _ => throw new RuntimeException($"Unknown value type received: '{value.GetType()}'"),
            };
        }
    }
}
