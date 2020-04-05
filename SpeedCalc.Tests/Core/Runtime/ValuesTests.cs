using SpeedCalc.Core.Runtime;

using System;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ValuesTests
    {
        [Fact]
        public void ValuesAreTheirOwnType()
        {
            Assert.True(Values.Bool(true).IsBool());
            Assert.True(Values.Bool(false).IsBool());

            Assert.True(Values.Number(1m).IsNumber());

            Assert.True(Values.String("").IsString());

            Assert.True(Values.Function(new object()).IsFunction());
        }

        [Fact]
        public void ValuesAreNotSomeOtherType()
        {
            var boolVal = Values.Bool(true);
            Assert.False(boolVal.IsNumber());
            Assert.False(boolVal.IsString());
            Assert.False(boolVal.IsFunction());

            var numberVal = Values.Number(0m);
            Assert.False(numberVal.IsBool());
            Assert.False(numberVal.IsString());
            Assert.False(numberVal.IsFunction());

            var stringVal = Values.String("");
            Assert.False(stringVal.IsBool());
            Assert.False(stringVal.IsNumber());
            Assert.False(stringVal.IsFunction());

            var funcVal = Values.Function(new object());
            Assert.False(funcVal.IsBool());
            Assert.False(funcVal.IsString());
            Assert.False(funcVal.IsNumber());
        }

        [Fact]
        public void ValueTypesRejectNull()
        {
            Assert.ThrowsAny<ArgumentException>(() => Values.IsBool(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsNumber(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsString(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsFunction(null));

            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsBool());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsNumber());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsString());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsFunction());
        }

        [Fact]
        public void ValuesRetainTheValueTheyAreGiven()
        {
            Assert.True(Values.Bool(true).AsBool());
            Assert.False(Values.Bool(false).AsBool());

            Assert.Equal(0m, Values.Number(0m).AsNumber());
            Assert.Equal(1m, Values.Number(1m).AsNumber());
            Assert.Equal(1000.500m, Values.Number(1000.500m).AsNumber());

            Assert.Equal("", Values.String("").AsString());
            Assert.Equal("test", Values.String("test").AsString());

            var tempFunctionValue = new object();
            Assert.Same(tempFunctionValue, Values.Function(tempFunctionValue).AsFunction());
        }

        [Fact]
        public void ValuesThrowWhenCallingInvalidUnwrapFunction()
        {
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsNumber());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsString());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsString());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.String("").AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.String("").AsNumber());
            Assert.Throws<RuntimeValueTypeException>(() => Values.String("").AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new object()).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new object()).AsString());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new object()).AsNumber());
        }

        [Fact]
        public void BoolValueEquality()
        {
            Assert.True(Values.Bool(true).EqualsValue(Values.Bool(true)));
            Assert.True(Values.Bool(false).EqualsValue(Values.Bool(false)));

            Assert.False(Values.Bool(true).EqualsValue(Values.Bool(false)));
            Assert.False(Values.Bool(false).EqualsValue(Values.Bool(true)));
        }

        [Fact]
        public void NumberValueEquality()
        {
            Assert.True(Values.Number(0m).EqualsValue(Values.Number(0m)));
            Assert.True(Values.Number(1.2345m).EqualsValue(Values.Number(1.2345m)));
            Assert.True(Values.Number(decimal.MaxValue).EqualsValue(Values.Number(decimal.MaxValue)));
            Assert.True(Values.Number(decimal.MinValue).EqualsValue(Values.Number(decimal.MinValue)));
            Assert.False(Values.Number(decimal.MinValue).EqualsValue(Values.Number(decimal.MaxValue)));
            Assert.False(Values.Number(0m).EqualsValue(Values.Number(1m)));
        }

        [Fact]
        public void FunctionValueEquality()
        {
            // TODO: Implement as soon as there are functions
        }

        [Fact]
        public void ValuesOfDifferentTypesAreNotEqual()
        {
            Assert.False(Values.Bool(true).EqualsValue(Values.String("")));
            Assert.False(Values.Bool(true).EqualsValue(Values.Number(0m)));
            Assert.False(Values.Bool(true).EqualsValue(Values.Function(new object())));
        }
    }
}
