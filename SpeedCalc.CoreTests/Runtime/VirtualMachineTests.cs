using SpeedCalc.Core.Runtime;

using System;

using Xunit;

namespace SpeedCalc.CoreTests.Runtime
{
    public class VirtualMachineTests
    {
        [Fact]
        public void PopFromEmptyStackThrows()
        {
            var vm = new VirtualMachine();

            Assert.Throws<RuntimeExecutionException>(() => vm.Pop());
        }

        [Fact]
        public void PushThrowsOnNullValue()
        {
            var vm = new VirtualMachine();

            Assert.ThrowsAny<ArgumentException>(() => vm.Push(null));
        }

        [Fact]
        public void PushAndPopWorkAsStack()
        {
            var vm = new VirtualMachine();

            var firstVal = Values.Bool(false);
            var secondVal = Values.Number(decimal.MinusOne);

            vm.Push(firstVal);
            vm.Push(secondVal);

            Assert.True(vm.Pop().EqualsValue(secondVal));
            Assert.True(vm.Pop().EqualsValue(firstVal));
        }

        [Fact]
        public void MachineThrowsOnNullChunk()
        {
            var vm = new VirtualMachine();

            Chunk chunk = null;
            Assert.ThrowsAny<ArgumentException>(() => vm.Interpret(chunk));
        }


        [Fact]
        public void MachineThrowsOnNullSourceString()
        {
            var vm = new VirtualMachine();

            string source = null;
            Assert.ThrowsAny<ArgumentException>(() => vm.Interpret(source));
        }

        [Fact]
        public void MachinePushesConstant()
        {
            var vm = new VirtualMachine();
            var chunk = new Chunk();
            var constValue = Values.Bool(true);
            var constIndex = chunk.AddConstant(constValue);
            chunk.Write(OpCode.Constant, (byte)constIndex, 1);
            chunk.Write(OpCode.Return, 1);

            vm.Interpret(chunk);

            Assert.True(vm.Pop().Equals(constValue));
        }

        [Fact]
        public void MachinePreservesStackAcrossInterpretCalls()
        {
            var vm = new VirtualMachine();

            // first chunk adds values onto stack
            var chunk = new Chunk();
            var value = Values.Number(1);
            var constIndex = chunk.AddConstant(value);
            chunk.Write(OpCode.Constant, (byte)constIndex, 1);
            chunk.Write(OpCode.Constant, (byte)constIndex, 1);
            chunk.Write(OpCode.Return, 1);

            // second chunk adds values already on stack
            var secondChunk = new Chunk();
            secondChunk.Write(OpCode.Add, 1);
            secondChunk.Write(OpCode.Return, 1);

            vm.Interpret(chunk);
            vm.Interpret(secondChunk);

            Assert.True(vm.Pop().EqualsValue(Values.Number(2)));
        }
    }
}
