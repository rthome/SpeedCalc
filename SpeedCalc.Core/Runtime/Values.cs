using System;

namespace SpeedCalc.Core.Runtime
{
    public interface IValue { }

    public static class Values
    {
        #region Value Subtypes

        sealed class NilVal : IValue { }

        sealed class BoolVal : IValue
        {
            public bool Value { get; }

            public BoolVal(bool value) => Value = value;
        }

        sealed class NumberVal : IValue
        {
            public decimal Value { get; }

            public NumberVal(decimal value) => Value = value;
        }

        sealed class FunctionVal : IValue
        {
            public object Value { get; }

            public FunctionVal(object value) => Value = value;
        }

        #endregion

        static readonly IValue NilInstance = new NilVal();
        static readonly IValue TrueInstance = new BoolVal(true);
        static readonly IValue FalseInstance = new BoolVal(false);

        public static IValue Nil() => NilInstance;

        public static IValue Bool(bool value) => value ? TrueInstance : FalseInstance;

        public static IValue Number(decimal value) => new NumberVal(value);

        public static IValue Function(object value) => new FunctionVal(value);

        public static bool IsNil(this IValue value) => value is NilVal;

        public static bool IsBool(this IValue value) => value is BoolVal;

        public static bool IsNumber(this IValue value) => value is NumberVal;

        public static bool IsFunction(this IValue value) => value is FunctionVal;

        public static bool AsBool(this IValue value)
        {
            if (value is BoolVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a bool value.");
        }

        public static decimal AsNumber(this IValue value)
        {
            if (value is NumberVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a number value.");
        }

        public static object AsFunction(this IValue value)
        {
            if (value is FunctionVal val)
                return val.Value;
            else
                throw new RuntimeValueTypeException($"Given runtime value '{value}' is not a function value.");
        }

        public static bool EqualsValue(this IValue firstValue, IValue secondValue)
        {
            if (firstValue.GetType() != secondValue.GetType())
                return false;

            switch(firstValue)
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

            throw new RuntimeException($"Unknown value types received: '{firstValue}' and {secondValue}'");
        }
    }
}
