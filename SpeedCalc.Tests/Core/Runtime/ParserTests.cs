using SpeedCalc.Core.Runtime;

using System;
using System.Collections.Generic;

using Xunit;

namespace SpeedCalc.Tests.Core.Runtime
{
    public class ParserTests
    {
        class Code
        {
            public class Seq
            {
                readonly Chunk chunk;
                readonly List<Action> ops = new List<Action>();
                int offset;
                private readonly bool toEnd;

                void MakeOp(int size, Action<int> testAction)
                {
                    var current = offset;
                    void op() => testAction(current);

                    offset += size;
                    ops.Add(op);
                }

                public Seq Instr(OpCode opcode)
                {
                    MakeOp(1, (current) => Assert.Equal(opcode, (OpCode)chunk.Code[current]));
                    return this;
                }

                public Seq Print() => Instr(OpCode.Print);

                public Seq Pop() => Instr(OpCode.Pop);

                public Seq Bool(bool value) => Instr(value ? OpCode.True : OpCode.False);

                public Seq Const(OpCode opcode, Value constVal)
                {
                    MakeOp(2, (current) =>
                    {
                        Assert.Equal(opcode, (OpCode)chunk.Code[current]);
                        Assert.True(constVal.EqualsValue(chunk.Constants[chunk.Code[current + 1]]));
                    });
                    return this;
                }

                public Seq String(string value) => Const(OpCode.Constant, Values.String(value));

                public Seq Number(decimal value) => Const(OpCode.Constant, Values.Number(value));

                public Seq Global(string name) => Const(OpCode.DefineGlobal, Values.String(name));

                public Seq LoadGlobal(string name) => Const(OpCode.LoadGlobal, Values.String(name));

                public Seq AssignGlobal(string name) => Const(OpCode.AssignGlobal, Values.String(name));

                public void Test()
                {
                    if (toEnd)
                        Instr(OpCode.Return);

                    foreach (var op in ops)
                        op();
                }

                public Seq(Chunk chunk, int offset, bool toEnd)
                {
                    this.chunk = chunk;
                    this.offset = offset;
                    this.toEnd = toEnd;
                }
            }

            public static Seq Compile(string source, int initialOffset = 0, bool checkToEnd = true)
            {
                var chunk = new Chunk();
                Parser.Compile(source, chunk);
                return new Seq(chunk, initialOffset, checkToEnd);
            }
        }

        [Fact]
        public void ParserCompilesNumberToConstant()
        {
            Code.Compile("100")
                .Number(100)
                .Pop()
                .Test();
        }

        [Fact]
        public void ParserCompilesLiterals()
        {
            Code.Compile("true")
                .Bool(true)
                .Pop()
                .Test();

            Code.Compile("false")
                .Bool(false)
                .Pop()
                .Test();
        }

        [Fact]
        public void ParserCompilesNotExpr()
        {
            Code.Compile("!true")
                .Bool(true)
                .Instr(OpCode.Not)
                .Pop()
                .Test();
        }

        [Fact]
        public void ParserCompilesNegateExpr()
        {
            Code.Compile("-1")
                .Number(1)
                .Instr(OpCode.Negate)
                .Pop()
                .Test();
        }

        [Fact]
        public void ParserCompilesPrintStmt()
        {
            Code.Compile("print 123;")
                .Number(123)
                .Print()
                .Test();
        }

        [Fact]
        public void ParserCompilesGlobalDefinitionStmt()
        {
            Code.Compile("var Global = 100;")
                .Number(100)
                .Global("Global")
                .Test();
        }

        [Fact]
        public void ParserCompilesGlobalLoadStmt()
        {
            Code.Compile("var Value = 123; print Value;", initialOffset: 4)
                .LoadGlobal("Value")
                .Print()
                .Test();
        }

        [Fact]
        public void ParserCompilesGlobalAssignStmt()
        {
            Code.Compile("var Value = 123; Value = true;", initialOffset: 4)
                .Bool(true)
                .AssignGlobal("Value")
                .Pop()
                .Test();
        }
    }
}
