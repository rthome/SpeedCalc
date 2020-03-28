using SpeedCalc.Core.Runtime;

using System;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class VirtualMachineTests
    {
        static void RunChunkWith(VirtualMachine vm, params OpCode[] opcodes)
        {
            var chunk = new Chunk();
            foreach (var op in opcodes)
                chunk.Write(op, 1);
            chunk.Write(OpCode.Return, 2);
            vm.Interpret(chunk);
        }

        [Fact]
        public void PopFromEmptyStackThrows()
        {
            var vm = new VirtualMachine();

            Assert.Throws<RuntimeExecutionException>(() => vm.Pop());
        }

        [Fact]
        public void PeekOnEmptyStackThrows()
        {
            var vm = new VirtualMachine();

            Assert.Throws<RuntimeExecutionException>(() => vm.Peek(0));
        }

        [Fact]
        public void PushThrowsOnNullValue()
        {
            var vm = new VirtualMachine();

            Assert.ThrowsAny<ArgumentException>(() => vm.Push(null));
        }

        [Fact]
        public void PeekBeyondStackBorderThrows()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Nil());
            vm.Push(Values.Nil());

            vm.Peek(0);
            vm.Peek(1);
            Assert.Throws<RuntimeExecutionException>(() => vm.Peek(2));
            Assert.Throws<RuntimeExecutionException>(() => vm.Peek(3));
            Assert.Throws<RuntimeExecutionException>(() => vm.Peek(1_000_000_000));
        }

        [Fact]
        public void PushAndPopAndPeekWorkAsStack()
        {
            var vm = new VirtualMachine();

            var firstVal = Values.Bool(false);
            var secondVal = Values.Number(decimal.MinusOne);

            vm.Push(firstVal);
            vm.Push(secondVal);

            Assert.True(vm.Peek(0).EqualsValue(secondVal));
            Assert.True(vm.Peek(1).EqualsValue(firstVal));
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
        public void MachinePushesPrimitiveValues()
        {
            var vm = new VirtualMachine();
            RunChunkWith(vm, OpCode.True, OpCode.False, OpCode.True, OpCode.Nil);

            Assert.True(vm.Pop().IsNil());
            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));
            Assert.True(vm.Pop().EqualsValue(Values.Bool(false)));
            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));
        }

        [Fact]
        public void MachineEquatesValues()
        {
            var vm = new VirtualMachine();
            RunChunkWith(vm, OpCode.False, OpCode.True, OpCode.False, OpCode.Equal, OpCode.Equal);

            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));
            Assert.ThrowsAny<RuntimeExecutionException>(() => vm.Pop());
        }

        [Fact]
        public void MachineNotsValues()
        {
            var vm = new VirtualMachine();

            RunChunkWith(vm, OpCode.Nil, OpCode.Not);
            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));

            RunChunkWith(vm, OpCode.False, OpCode.Not);
            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));

            vm.Push(Values.Number(0));
            RunChunkWith(vm, OpCode.Not);
            Assert.True(vm.Pop().EqualsValue(Values.Bool(true)));

            RunChunkWith(vm, OpCode.True, OpCode.Not);
            Assert.True(vm.Pop().EqualsValue(Values.Bool(false)));

            vm.Push(Values.Number(1234));
            RunChunkWith(vm, OpCode.Not);
            Assert.True(vm.Pop().EqualsValue(Values.Bool(false)));
        }

        [Fact]
        public void MachineAddsNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(5));
            vm.Push(Values.Number(2));

            RunChunkWith(vm, OpCode.Add);

            Assert.True(vm.Pop().EqualsValue(Values.Number(7)));
        }

        [Fact]
        public void MachineSubtractsNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(2));
            vm.Push(Values.Number(5));

            RunChunkWith(vm, OpCode.Subtract);

            Assert.True(vm.Pop().EqualsValue(Values.Number(3)));
        }

        [Fact]
        public void MachineMultipliesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(5));
            vm.Push(Values.Number(2));

            RunChunkWith(vm, OpCode.Multiply);

            Assert.True(vm.Pop().EqualsValue(Values.Number(10)));
        }

        [Fact]
        public void MachineDividesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(2));
            vm.Push(Values.Number(6));

            RunChunkWith(vm, OpCode.Divide);

            Assert.True(vm.Pop().EqualsValue(Values.Number(3)));
        }

        [Fact]
        public void MachineChecksTypesForBinaryOps()
        {
            {
                var vm = new VirtualMachine();
                vm.Push(Values.Number(1));

                Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.True, OpCode.Add));
            }

            {
                var vm = new VirtualMachine();
                vm.Push(Values.Number(1));

                Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.Nil, OpCode.Subtract));
            }

            {
                var vm = new VirtualMachine();
                Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.True, OpCode.False, OpCode.Multiply));
            }

            {
                var vm = new VirtualMachine();
                Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.Nil, OpCode.False, OpCode.Divide));
            }
        }

        [Fact]
        public void MachineNegatesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(1));

            RunChunkWith(vm, OpCode.Negate);
            Assert.True(vm.Peek().EqualsValue(Values.Number(-1)));

            RunChunkWith(vm, OpCode.Negate);
            Assert.True(vm.Pop().EqualsValue(Values.Number(1)));
        }

        [Fact]
        public void MachineChecksTypeForNegate()
        {
            var vm = new VirtualMachine();
            Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.Nil, OpCode.Negate));
            Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.True, OpCode.Negate));
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
