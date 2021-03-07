﻿using SpeedCalc.Core.Runtime;

using System;
using System.IO;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
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
        public void PopNPopsCorrectAmount()
        {
            var vm = new VirtualMachine();

            var first = Values.Number(1);
            var second = Values.Number(1);
            var third = Values.Number(1);

            vm.Push(first);
            vm.Push(second);
            vm.Push(third);

            vm.PopN(2);
            Assert.Equal(first, vm.Pop());
        }

        [Fact]
        public void MachineThrowsOnNullChunk()
        {
            var vm = new VirtualMachine();

            Assert.ThrowsAny<ArgumentException>(() => vm.Interpret(null));
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
    }
}
