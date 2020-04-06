using SpeedCalc.Core.Runtime;

using System;
using System.IO;
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
            vm.Push(Values.Bool(true));
            vm.Push(Values.Bool(false));

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

            Assert.Equal(vm.Peek(0), secondVal);
            Assert.Equal(vm.Peek(1), firstVal);
            Assert.Equal(vm.Pop(), secondVal);
            Assert.Equal(vm.Pop(), firstVal);
        }

        [Fact]
        public void MachineThrowsOnNullChunk()
        {
            var vm = new VirtualMachine();

            Chunk chunk = null;
            Assert.ThrowsAny<ArgumentException>(() => vm.Interpret(chunk));
        }

        [Fact]
        public void MachineThrowsOnNullStdOut()
        {
            var vm = new VirtualMachine();

            Assert.ThrowsAny<ArgumentException>(() => vm.SetStdOut(null));
        }

        [Fact]
        public void MachineDefaultStdOutToConsole()
        {
            var vm = new VirtualMachine();

            Assert.Same(Console.Out, vm.StdOut);
        }

        [Fact]
        public void MachineSetsGivenStdOut()
        {
            var vm = new VirtualMachine();
            var writer = new StringWriter();

            vm.SetStdOut(writer);
            Assert.Same(writer, vm.StdOut);
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

            Assert.Equal(vm.Pop(), constValue);
        }

        [Fact]
        public void MachinePushesPrimitiveValues()
        {
            var vm = new VirtualMachine();
            RunChunkWith(vm, OpCode.True, OpCode.False, OpCode.True);

            Assert.True(vm.Pop().AsBool());
            Assert.False(vm.Pop().AsBool());
            Assert.True(vm.Pop().AsBool());
        }

        [Fact]
        public void MachineEquatesValues()
        {
            var vm = new VirtualMachine();
            RunChunkWith(vm, OpCode.False, OpCode.True, OpCode.False, OpCode.Equal, OpCode.Equal);

            Assert.True(vm.Pop().AsBool());
            Assert.ThrowsAny<RuntimeExecutionException>(() => vm.Pop());
        }

        [Fact]
        public void MachineNotsValues()
        {
            var vm = new VirtualMachine();

            RunChunkWith(vm, OpCode.False, OpCode.Not);
            Assert.True(vm.Pop().AsBool());

            vm.Push(Values.Number(0));
            RunChunkWith(vm, OpCode.Not);
            Assert.True(vm.Pop().AsBool());

            RunChunkWith(vm, OpCode.True, OpCode.Not);
            Assert.False(vm.Pop().AsBool());

            vm.Push(Values.Number(1234));
            RunChunkWith(vm, OpCode.Not);
            Assert.False(vm.Pop().AsBool());
        }

        [Fact]
        public void MachineAddsNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(2));
            vm.Push(Values.Number(5));

            RunChunkWith(vm, OpCode.Add);

            Assert.Equal(7M, vm.Pop().AsNumber());
        }

        [Fact]
        public void MachineSubtractsNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(5));
            vm.Push(Values.Number(2));

            RunChunkWith(vm, OpCode.Subtract);

            Assert.Equal(3M, vm.Pop().AsNumber());
        }

        [Fact]
        public void MachineMultipliesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(2));
            vm.Push(Values.Number(5));

            RunChunkWith(vm, OpCode.Multiply);

            Assert.Equal(10M, vm.Pop().AsNumber());
        }

        [Fact]
        public void MachineDividesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(6));
            vm.Push(Values.Number(2));

            RunChunkWith(vm, OpCode.Divide);

            Assert.Equal(3M, vm.Pop().AsNumber());
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
                Assert.ThrowsAny<RuntimeExecutionException>(() => RunChunkWith(vm, OpCode.True, OpCode.False, OpCode.Multiply));
            }
        }

        [Fact]
        public void MachineNegatesNumbers()
        {
            var vm = new VirtualMachine();
            vm.Push(Values.Number(1));

            RunChunkWith(vm, OpCode.Negate);
            Assert.Equal(-1M, vm.Peek().AsNumber());

            RunChunkWith(vm, OpCode.Negate);
            Assert.Equal(1M, vm.Pop().AsNumber());
        }

        [Fact]
        public void MachineChecksTypeForNegate()
        {
            var vm = new VirtualMachine();
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

            Assert.Equal(2M, vm.Pop().AsNumber());
        }
    }
}
