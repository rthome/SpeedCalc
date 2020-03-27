using SpeedCalc.Core.Runtime;

using System;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ChunkTests
    {
        [Fact]
        public void ChunkShouldInitializeAsEmpty()
        {
            var chunk = new Chunk();

            Assert.Equal(0, chunk.Code.Count);
            Assert.Equal(0, chunk.Lines.Count);
            Assert.Equal(0, chunk.Constants.Count);
        }

        [Fact]
        public void ChunkWritesBytesAndLines()
        {
            var chunk = new Chunk();
            chunk.Write(1, 10);
            chunk.Write(2, 20);
            chunk.Write(3, 30);

            Assert.Equal(new byte[] { 1, 2, 3 }, chunk.Code);
            Assert.Equal(new int[] { 10, 20, 30 }, chunk.Lines);
        }

        [Fact]
        public void ChunkWritesOpcodeByteOnWrite()
        {
            var chunk = new Chunk();
            chunk.Write(OpCode.Add, 1);

            Assert.Equal(OpCode.Add, (OpCode)chunk.Code[0]);
        }

        [Fact]
        public void ChunkWritesOpcodeAndArg()
        {
            var chunk = new Chunk();
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Return, 10, 2);

            Assert.Equal(new byte[] { (byte)OpCode.Constant, 0, (byte)OpCode.Return, 10 }, chunk.Code);
            Assert.Equal(new[] { 1, 1, 2, 2 }, chunk.Lines);
        }

        [Fact]
        public void ChunkChecksForNullConstants()
        {
            var chunk = new Chunk();

            Assert.ThrowsAny<ArgumentException>(() => chunk.AddConstant(null));
        }

        [Fact]
        public void ChunkWritesConstants()
        {
            var chunk = new Chunk();
            chunk.AddConstant(Values.Nil());
            chunk.AddConstant(Values.Bool(true));

            Assert.True(chunk.Constants[0].EqualsValue(Values.Nil()));
            Assert.True(chunk.Constants[1].EqualsValue(Values.Bool(true)));
        }

        [Fact]
        public void ChunkReturnsConstantIndices()
        {
            var chunk = new Chunk();
            Assert.Equal(0, chunk.AddConstant(Values.Nil()));
            Assert.Equal(1, chunk.AddConstant(Values.Number(0m)));
        }
    }
}
