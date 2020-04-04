using SpeedCalc.Core.Runtime;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ParserTests
    {
        [Fact]
        public void ParserCompilesNumberToConstant()
        {
            var chunk = new Chunk();
            Parser.Compile("100", chunk);

            Assert.Equal((byte)OpCode.Constant, chunk.Code[0]);
            Assert.Equal((byte)0, chunk.Code[1]);
            Assert.Equal(1, chunk.Constants.Count);
            Assert.True(chunk.Constants[0].EqualsValue(Values.Number(100)));
        }

        [Fact]
        public void ParserCompilesLiterals()
        {
            var trueChunk = new Chunk();
            Parser.Compile("true", trueChunk);

            var falseChunk = new Chunk();
            Parser.Compile("false", falseChunk);

            Assert.Equal((byte)OpCode.True, trueChunk.Code[0]);
            Assert.Equal(0, trueChunk.Constants.Count);
            Assert.Equal((byte)OpCode.False, falseChunk.Code[0]);
            Assert.Equal(0, falseChunk.Constants.Count);
        }

        [Fact]
        public void ParserCompilesNotExpr()
        {
            var chunk = new Chunk();
            Parser.Compile("!true", chunk);

            Assert.Equal((byte)OpCode.True, chunk.Code[0]);
            Assert.Equal((byte)OpCode.Not, chunk.Code[1]);
            Assert.Equal(0, chunk.Constants.Count);
        }

        [Fact]
        public void ParserCompilesNegateExpr()
        {
            var chunk = new Chunk();
            Parser.Compile("-1", chunk);

            Assert.Equal((byte)OpCode.Constant, chunk.Code[0]);
            Assert.Equal((byte)0, chunk.Code[1]);
            Assert.Equal((byte)OpCode.Negate, chunk.Code[2]);
            Assert.Equal(1, chunk.Constants.Count);
        }

        [Fact]
        public void ParserCompilesPrintStmt()
        {
            var chunk = new Chunk();
            Parser.Compile("print 123;", chunk);

            Assert.Equal((byte)OpCode.Constant, chunk.Code[0]);
            Assert.Equal((byte)0, chunk.Code[1]);
            Assert.Equal((byte)OpCode.Print, chunk.Code[2]);
        }
    }
}
