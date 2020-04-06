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

                public Seq ConstInstr(OpCode opcode, Value constVal)
                {
                    MakeOp(2, (current) =>
                    {
                        Assert.Equal(opcode, (OpCode)chunk.Code[current]);
                        Assert.Equal(constVal, chunk.Constants[chunk.Code[current + 1]]);
                    });
                    return this;
                }

                public Seq Constant(Value constVal) => ConstInstr(OpCode.Constant, constVal);

                public Seq String(string value) => Constant(Values.String(value));

                public Seq Number(decimal value) => Constant(Values.Number(value));

                public Seq Global(string name) => ConstInstr(OpCode.DefineGlobal, Values.String(name));

                public Seq LoadGlobal(string name) => ConstInstr(OpCode.LoadGlobal, Values.String(name));

                public Seq AssignGlobal(string name) => ConstInstr(OpCode.AssignGlobal, Values.String(name));

                public Seq SlotInstr(OpCode opcode, int slot)
                {
                    MakeOp(2, (current) =>
                    {
                        Assert.Equal(opcode, (OpCode)chunk.Code[current]);
                        Assert.Equal(slot, chunk.Code[current + 1]);
                    });
                    return this;
                }

                public Seq LoadLocal(int slot) => SlotInstr(OpCode.LoadLocal, slot);

                public Seq AssignLocal(int slot) => SlotInstr(OpCode.AssignLocal, slot);

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
                Assert.True(Parser.Compile(source, chunk));
                return new Seq(chunk, initialOffset, checkToEnd);
            }
        }

        [Fact]
        public void NumberToConstant()
        {
            Code.Compile("100;")
                .Number(100)
                .Pop()
                .Test();
        }

        [Fact]
        public void Literals()
        {
            Code.Compile("true;")
                .Bool(true)
                .Pop()
                .Test();

            Code.Compile("false;")
                .Bool(false)
                .Pop()
                .Test();
        }

        [Fact]
        public void Not()
        {
            Code.Compile("!true;")
                .Bool(true)
                .Instr(OpCode.Not)
                .Pop()
                .Test();
        }

        [Fact]
        public void Negate()
        {
            Code.Compile("-1;")
                .Number(1)
                .Instr(OpCode.Negate)
                .Pop()
                .Test();
        }

        [Fact]
        public void Print()
        {
            Code.Compile("print 123;")
                .Number(123)
                .Print()
                .Test();
        }

        [Fact]
        public void GlobalDefinition()
        {
            Code.Compile("var Global = 100;")
                .Number(100)
                .Global("Global")
                .Test();
        }

        [Fact]
        public void GlobalLoadStmt()
        {
            Code.Compile("var Value = 123; print Value;", initialOffset: 4)
                .LoadGlobal("Value")
                .Print()
                .Test();
        }

        [Fact]
        public void GlobalAssign()
        {
            Code.Compile("var Value = 123; Value = true;", initialOffset: 4)
                .Bool(true)
                .AssignGlobal("Value")
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalDefinition()
        {
            Code.Compile("{ var a = 1; }")
                .Number(1)
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalPrint()
        {
            Code.Compile("{ var a = 1; print a; }")
                .Number(1)
                .LoadLocal(0)
                .Print()
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalAssignFromArithmeticExpr()
        {
            Code.Compile("{ var a = 2 * (3 + 4 / 5**2); print a; }")
                .Number(2)
                .Number(3)
                .Number(4)
                .Number(5)
                .Number(2)
                .Instr(OpCode.Exp)
                .Instr(OpCode.Divide)
                .Instr(OpCode.Add)
                .Instr(OpCode.Multiply)
                .LoadLocal(0)
                .Print()
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalAssignFromLocal()
        {
            Code.Compile("{ var a = 1; var b = a; }")
                .Number(1)
                .LoadLocal(0)
                .Pop()
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalAssignFromGlobal()
        {
            Code.Compile("var g = true; { var a = g; }")
                .Bool(true)
                .Global("g")
                .LoadGlobal("g")
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalAssignFromLocalAndPrint()
        {
            Code.Compile("{ var a = 1; var b = a; print b; }")
                .Number(1)
                .LoadLocal(0)
                .LoadLocal(1)
                .Print()
                .Pop()
                .Pop()
                .Test();
        }
    }
}
