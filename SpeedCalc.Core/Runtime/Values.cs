using System;
using System.Globalization;

namespace SpeedCalc.Core.Runtime
{
    public abstract class Value
    {
        public override sealed string ToString() => Values.ToString(this);
    }

    public static class Values
    {
        #region Value Subtypes

        sealed class BoolVal : Value
        {
            public bool Value { get; }

            public BoolVal(bool value) => Value = value;
        }

        sealed class NumberVal : Value
        {
            public decimal Value { get; }

            public NumberVal(decimal value) => Value = value;
        }

        sealed class StringVal : Value
        {
            public string Value { get; }

            public StringVal(string value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        sealed class FunctionVal : Value
        {
            public object Value { get; }

            public FunctionVal(object value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        #endregion

        static readonly Value TrueInstance = new BoolVal(true);
        static readonly Value FalseInstance = new BoolVal(false);

        public static Value Bool(bool value) => value ? TrueInstance : FalseInstance;

        public static Value Number(decimal value) => new NumberVal(value);

        public static Value String(string value) => new StringVal(value);

        public static Value Function(object value) => new FunctionVal(value);

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

        public static object AsFunction(this Value value)
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

            switch (firstValue)
            {
                case BoolVal _:
                    return firstValue.AsBool() == secondValue.AsBool();
                case NumberVal _:
                    return firstValue.AsNumber() == secondValue.AsNumber();
                case StringVal _:
                    return firstValue.AsString() == secondValue.AsString();
                case FunctionVal _:
                    return firstValue.AsFunction() == secondValue.AsFunction();
            }

            throw new RuntimeException($"Unknown value types received: '{firstValue.GetType()}' and {secondValue.GetType()}'");
        }

        public static string ToString(this Value value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            switch (value)
            {
                case BoolVal _:
                    return value.AsBool() ? "true" : "false";
                case NumberVal _:
                    return value.AsNumber().ToString("G", CultureInfo.InvariantCulture);
                case StringVal val:
                    return val.Value;
                case FunctionVal _:
                    return "<function>";
            }

            throw new RuntimeException($"Unknown value type received: '{value.GetType()}'");
        }
    }
}
