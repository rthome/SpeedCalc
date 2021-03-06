﻿using SpeedCalc.Core.Runtime;

using System;
using System.Collections.Generic;
using System.Diagnostics;

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
                readonly List<Action> ops = new();
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
                    MakeOp(1, (current) => Assert.Equal((current, opcode), (current, (OpCode)chunk.Code[current])));
                    return this;
                }

                public Seq Print() => Instr(OpCode.Print);

                public Seq Pop() => Instr(OpCode.Pop);

                public Seq Bool(bool value) => Instr(value ? OpCode.True : OpCode.False);

                public Seq ConstInstr(OpCode opcode, Value constVal)
                {
                    MakeOp(2, (current) => Assert.Equal((current, opcode, constVal), (current, (OpCode)chunk.Code[current], chunk.Constants[chunk.Code[current + 1]])));
                    return this;
                }

                public Seq Constant(Value constVal) => ConstInstr(OpCode.Constant, constVal);

                public Seq String(string value) => Constant(Values.String(value));

                public Seq Number(decimal value) => Constant(Values.Number(value));

                public Seq Global(string name) => ConstInstr(OpCode.DefineGlobal, Values.String(name));

                public Seq LoadGlobal(string name) => ConstInstr(OpCode.LoadGlobal, Values.String(name));

                public Seq AssignGlobal(string name) => ConstInstr(OpCode.AssignGlobal, Values.String(name));

                public Seq ArgInstr(OpCode opcode, byte arg)
                {
                    MakeOp(2, (current) => Assert.Equal((current, opcode, arg), (current, (OpCode)chunk.Code[current], chunk.Code[current + 1])));
                    return this;
                }

                public Seq PopN(byte count) => ArgInstr(OpCode.PopN, count);

                public Seq LoadLocal(byte slot) => ArgInstr(OpCode.LoadLocal, slot);

                public Seq AssignLocal(byte slot) => ArgInstr(OpCode.AssignLocal, slot);

                public Seq OffsetInstr(OpCode opcode, int arg)
                {
                    var firstByte = (byte)((arg >> 8) & 0xff);
                    var secondByte = (byte)(arg & 0xff);
                    MakeOp(3, (current) => Assert.Equal((current, opcode, firstByte, secondByte), (current, (OpCode)chunk.Code[current], chunk.Code[current + 1], chunk.Code[current + 2])));
                    return this;
                }

                public Seq JumpIfFalse(int offset) => OffsetInstr(OpCode.JumpIfFalse, offset);

                public Seq Jump(int offset) => OffsetInstr(OpCode.Jump, offset);

                public Seq Loop(int offset) => OffsetInstr(OpCode.Loop, offset);

                public void Test()
                {
                    if (toEnd)
                    {
                        Instr(OpCode.False);
                        Instr(OpCode.Return);
                    }

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
                var parser = new Parser();
                var function = parser.Compile(source);
                Assert.NotNull(function);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(Debugger.IsAttached, function.DisassembleFunction());
#endif

                return new Seq(function.Chunk, initialOffset, checkToEnd);
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
        public void Modulo()
        {
            Code.Compile("5 mod 2;")
                .Number(5)
                .Number(2)
                .Instr(OpCode.Modulo)
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
                .LoadLocal(1)
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
                .LoadLocal(1)
                .Print()
                .Pop()
                .Test();
        }

        [Fact]
        public void LocalAssignFromLocal()
        {
            Code.Compile("{ var a = 1; var b = a; }")
                .Number(1)
                .LoadLocal(1)
                .PopN(2)
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
                .LoadLocal(1)
                .LoadLocal(2)
                .Print()
                .PopN(2)
                .Test();
        }

        [Fact]
        public void IfWithoutElse()
        {
            Code.Compile("if true: {}")
                .Bool(true)
                .JumpIfFalse(4)
                .Pop()
                .Jump(1)
                .Pop()
                .Test();
        }

        [Fact]
        public void IfPrintWithoutElse()
        {
            Code.Compile("if true: print 1;")
                .Bool(true)
                .JumpIfFalse(7)
                .Pop()
                .Number(1)
                .Print()
                .Jump(1)
                .Pop()
                .Test();
        }

        [Fact]
        public void IfPrintWithElsePrint()
        {
            Code.Compile("if true: print 1; else: print 2;")
                .Bool(true)
                .JumpIfFalse(7)
                .Pop()
                .Number(1)
                .Print()
                .Jump(4)
                .Pop()
                .Number(2)
                .Print()
                .Test();
        }

        [Fact]
        public void AndWithTrueAndFalse()
        {
            Code.Compile("if true and false: print 1;")
                .Bool(true)
                .JumpIfFalse(2)
                .Pop()
                .Bool(false)
                .JumpIfFalse(7)
                .Pop()
                .Number(1)
                .Print()
                .Jump(1)
                .Pop()
                .Test();
        }

        [Fact]
        public void OrWithFalseAndTrueAndFalse()
        {
            Code.Compile("if false or true or false: print 1;")
                .Bool(false)
                .JumpIfFalse(3)
                .Jump(10)
                .Pop()
                .Bool(true)
                .JumpIfFalse(3)
                .Jump(2)
                .Pop()
                .Bool(false)
                .JumpIfFalse(7)
                .Pop()
                .Number(1)
                .Print()
                .Jump(1)
                .Pop()
                .Test();
        }

        [Fact]
        public void OrWithAssignmentFromThreeBools()
        {
            Code.Compile("{ var a = false or false or true; }")
                .Bool(false)
                .JumpIfFalse(3)
                .Jump(10)
                .Pop()
                .Bool(false)
                .JumpIfFalse(3)
                .Jump(2)
                .Pop()
                .Bool(true)
                .Pop()
                .Test();
        }

        [Fact]
        public void SimpleWhileLoop()
        {
            Code.Compile("while true: {}")
                .Bool(true)
                .JumpIfFalse(4)
                .Pop()
                .Loop(8)
                .Pop()
                .Test();
        }

        [Fact]
        public void IncrementingWhileLoop()
        {
            Code.Compile("{ var c = 0; while c < 10: c = c + 1; }")
                .Constant(Values.Number(0))
                .LoadLocal(1)
                .Constant(Values.Number(10))
                .Instr(OpCode.Less)
                .JumpIfFalse(12)
                .Pop()
                .LoadLocal(1)
                .Constant(Values.Number(1))
                .Instr(OpCode.Add)
                .AssignLocal(1)
                .Pop()
                .Loop(20)
                .Pop()
                .Pop()
                .Test();
        }
    }
}
