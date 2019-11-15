using SpeedCalc.Core.Runtime;

using Xunit;

namespace SpeedCalc.CoreTests.Runtime
{
    public class ValuesTests
    {
        [Fact]
        public void ValuesAreTheirOwnType()
        {
            Assert.True(Values.IsNil(Values.Nil()));

            Assert.True(Values.Bool(true).IsBool());
            Assert.True(Values.Bool(false).IsBool());

            Assert.True(Values.Number(1m).IsNumber());

            Assert.True(Values.Function(new object()).IsFunction());
        }

        [Fact]
        public void ValuesAreNotSomeOtherType()
        {
            var nilVal = Values.Nil();
            Assert.False(nilVal.IsBool());
            Assert.False(nilVal.IsNumber());
            Assert.False(nilVal.IsFunction());

            var boolVal = Values.Bool(true);
            Assert.False(boolVal.IsNil());
            Assert.False(boolVal.IsNumber());
            Assert.False(boolVal.IsFunction());

            var numberVal = Values.Number(0m);
            Assert.False(numberVal.IsNil());
            Assert.False(numberVal.IsBool());
            Assert.False(numberVal.IsFunction());

            var funcVal = Values.Function(null);
            Assert.False(funcVal.IsNil());
            Assert.False(funcVal.IsBool());
            Assert.False(funcVal.IsNumber());
        }

        [Fact]
        public void ValuesRetainTheValueTheyAreGiven()
        {
            Assert.True(Values.Bool(true).AsBool());
            Assert.False(Values.Bool(false).AsBool());

            Assert.Equal(0m, Values.Number(0m).AsNumber());
            Assert.Equal(1m, Values.Number(1m).AsNumber());
            Assert.Equal(1000.500m, Values.Number(1000.500m).AsNumber());

            Assert.Null(Values.Function(null).AsFunction());
        }

        [Fact]
        public void ValuesThrowWhenCallingInvalidUnwrapFunction()
        {
            Assert.Throws<RuntimeValueTypeException>(() => Values.Nil().AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Nil().AsNumber());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Nil().AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsNumber());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsFunction());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new object()).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new object()).AsNumber());
        }

        [Fact]
        public void NilValueEquality()
        {
            Assert.True(Values.Nil().EqualsValue(Values.Nil()));
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
            Assert.False(Values.Nil().EqualsValue(Values.Bool(false)));
            Assert.False(Values.Nil().EqualsValue(Values.Number(0m)));
            Assert.False(Values.Nil().EqualsValue(Values.Function(new object())));

            Assert.False(Values.Bool(true).EqualsValue(Values.Nil()));
            Assert.False(Values.Bool(true).EqualsValue(Values.Number(0m)));
            Assert.False(Values.Bool(true).EqualsValue(Values.Function(new object())));
        }
    }
}
