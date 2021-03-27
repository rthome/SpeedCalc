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

            Assert.True(Values.Function(new Function("testfunc", 0)).IsFunction());

            Assert.True(Values.NativeFunction(args => Values.Bool(true), 0).IsNativeFunction());
        }

        [Fact]
        public void ValuesAreNotSomeOtherType()
        {
            var boolVal = Values.Bool(true);
            Assert.False(boolVal.IsNumber());
            Assert.False(boolVal.IsString());
            Assert.False(boolVal.IsFunction());
            Assert.False(boolVal.IsNativeFunction());

            var numberVal = Values.Number(0m);
            Assert.False(numberVal.IsBool());
            Assert.False(numberVal.IsString());
            Assert.False(numberVal.IsFunction());
            Assert.False(numberVal.IsNativeFunction());

            var stringVal = Values.String("");
            Assert.False(stringVal.IsBool());
            Assert.False(stringVal.IsNumber());
            Assert.False(stringVal.IsFunction());
            Assert.False(stringVal.IsNativeFunction());

            var funcVal = Values.Function(new Function("myFunction", 2));
            Assert.False(funcVal.IsBool());
            Assert.False(funcVal.IsString());
            Assert.False(funcVal.IsNumber());
            Assert.False(funcVal.IsNativeFunction());

            var nativeFuncVal = Values.NativeFunction(args => Values.Bool(true), 0);
            Assert.False(nativeFuncVal.IsBool());
            Assert.False(nativeFuncVal.IsString());
            Assert.False(nativeFuncVal.IsNumber());
            Assert.False(nativeFuncVal.IsFunction());
        }

        [Fact]
        public void ValueTypesRejectNull()
        {
            Assert.ThrowsAny<ArgumentException>(() => Values.IsBool(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsNumber(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsString(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsFunction(null));
            Assert.ThrowsAny<ArgumentException>(() => Values.IsNativeFunction(null));

            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsBool());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsNumber());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsString());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsFunction());
            Assert.ThrowsAny<ArgumentException>(() => ((Value)null).AsNativeFunction());
        }

        [Fact]
        public void ValueEqualMemberAndExtensionAreEquivalent()
        {
            Assert.Equal(Values.Bool(true).Equals(Values.Bool(true)), Values.Bool(true).EqualsValue(Values.Bool(true)));
            Assert.Equal(Values.Number(123).Equals(Values.Number(123)), Values.Number(123).EqualsValue(Values.Number(123)));
            Assert.Equal(Values.Number(123).Equals(Values.Bool(false)), Values.Number(123).EqualsValue(Values.Bool(false)));

            Assert.Equal(Values.Number(123).Equals((object)Values.Bool(false)), Values.Number(123).EqualsValue(Values.Bool(false)));
            Assert.Equal(Values.Bool(true).Equals((object)Values.Bool(true)), Values.Bool(true).EqualsValue(Values.Bool(true)));
        }

        [Fact]
        public void ValuesDoNotEqualUnwrappedValues()
        {
            Assert.False(Values.Bool(true).Equals(null));
            Assert.False(Values.Bool(true).Equals(true));
            Assert.False(Values.String("test").Equals("test"));

            var func = new Function("test", 0);
            Assert.False(Values.Function(func).Equals(func));
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

            var func = new Function("test", 0);
            Assert.Equal(func, Values.Function(func).AsFunction());

            static Value nativeFunc(Value[] args) => Values.Bool(true);
            var nativeFuncDef = new Values.NativeFuncDefinition(nativeFunc, 0);
            Assert.Equal(nativeFuncDef, Values.NativeFunction(nativeFunc, 0).AsNativeFunction());
        }

        [Fact]
        public void ValuesThrowWhenCallingInvalidUnwrapFunction()
        {
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsNumber());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Bool(true).AsString());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Number(0m).AsString());

            Assert.Throws<RuntimeValueTypeException>(() => Values.String("").AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.String("").AsNumber());

            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new Function("func", 1)).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new Function("func", 1)).AsString());
            Assert.Throws<RuntimeValueTypeException>(() => Values.Function(new Function("func", 1)).AsNumber());

            Assert.Throws<RuntimeValueTypeException>(() => Values.NativeFunction(args => Values.Bool(true), 0).AsBool());
            Assert.Throws<RuntimeValueTypeException>(() => Values.NativeFunction(args => Values.Bool(true), 0).AsString());
            Assert.Throws<RuntimeValueTypeException>(() => Values.NativeFunction(args => Values.Bool(true), 0).AsNumber());
        }

        [Fact]
        public void BoolValueEquality()
        {
            Assert.Equal(Values.Bool(true), Values.Bool(true));
            Assert.Equal(Values.Bool(false), Values.Bool(false));

            Assert.NotEqual(Values.Bool(true), Values.Bool(false));
            Assert.NotEqual(Values.Bool(false), Values.Bool(true));
        }

        [Fact]
        public void NumberValueEquality()
        {
            Assert.Equal(Values.Number(0m), Values.Number(0m));
            Assert.Equal(Values.Number(1.2345m), Values.Number(1.2345m));
            Assert.Equal(Values.Number(decimal.MaxValue), Values.Number(decimal.MaxValue));
            Assert.Equal(Values.Number(decimal.MinValue), Values.Number(decimal.MinValue));
            Assert.NotEqual(Values.Number(decimal.MinValue), Values.Number(decimal.MaxValue));
            Assert.NotEqual(Values.Number(0m), Values.Number(1m));
        }

        [Fact]
        public void FunctionValueEquality()
        {
            var f1 = new Function("sine", 1);
            var func1 = Values.Function(f1);
            var func2 = Values.Function(new Function("cosine", 1));
            var func1Ref = func1;

            Assert.Equal(func1, func1);
            Assert.Equal(func1, func1Ref);
            Assert.Equal(func1, Values.Function(f1));
            Assert.NotEqual(func1, func2);
        }

        [Fact]
        public void NativeFunctionValueEquality()
        {
            static Value nativeFunc1(Value[] args) => Values.Bool(true);
            static Value nativeFunc2(Value[] args) => Values.Bool(false);


            var val1 = Values.NativeFunction(nativeFunc1, 0);
            var val2 = Values.NativeFunction(nativeFunc2, 0);
            var val1Ref = val1;

            Assert.Equal(val1, val1);
            Assert.Equal(val1, val1Ref);
            Assert.Equal(val1, Values.NativeFunction(nativeFunc1, 0));
            Assert.NotEqual(val1, val2);
        }

        [Fact]
        public void ValuesOfDifferentTypesAreNotEqual()
        {
            Assert.NotEqual(Values.Bool(true), Values.String(""));
            Assert.NotEqual(Values.Bool(true), Values.Number(0m));
            Assert.NotEqual(Values.Bool(true), Values.Function(new Function("test", 0)));
            Assert.NotEqual(Values.Bool(true), Values.NativeFunction(args => Values.Bool(true), 0));
        }
    }
}
