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

        sealed class NilVal : Value { }

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

        sealed class FunctionVal : Value
        {
            public object Value { get; }

            public FunctionVal(object value) => Value = value;
        }

        #endregion

        static readonly Value NilInstance = new NilVal();
        static readonly Value TrueInstance = new BoolVal(true);
        static readonly Value FalseInstance = new BoolVal(false);

        public static Value Nil() => NilInstance;

        public static Value Bool(bool value) => value ? TrueInstance : FalseInstance;

        public static Value Number(decimal value) => new NumberVal(value);

        public static Value Function(object value) => new FunctionVal(value);

        public static bool IsNil(this Value value) => value is NilVal;

        public static bool IsBool(this Value value) => value is BoolVal;

        public static bool IsNumber(this Value value) => value is NumberVal;

        public static bool IsFunction(this Value value) => value is FunctionVal;

        public static bool AsBool(this Value value)
        {
            if (value is BoolVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a bool value.");
        }

        public static decimal AsNumber(this Value value)
        {
            if (value is NumberVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a number value.");
        }

        public static object AsFunction(this Value value)
        {
            if (value is FunctionVal val)
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
                case NilVal _:
                    return true;
                case BoolVal _:
                    return firstValue.AsBool() == secondValue.AsBool();
                case NumberVal _:
                    return firstValue.AsNumber() == secondValue.AsNumber();
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
                case NilVal _:
                    return "nil";
                case BoolVal _:
                    return value.AsBool() ? "true" : "false";
                case NumberVal _:
                    return value.AsNumber().ToString("G", CultureInfo.InvariantCulture);
                case FunctionVal _:
                    return "function";
            }

            throw new RuntimeException($"Unknown value type received: '{value.GetType()}'");
        }
    }
}
