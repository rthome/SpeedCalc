using System;
using System.Collections.Generic;
using System.IO;

namespace SpeedCalc.Core.Runtime
{
    public sealed class VirtualMachine
    {
        const int MaxStackSize = 256;

        readonly Value[] stack;
        readonly Dictionary<string, Value> globals;

        Chunk executingChunk;
        int ipOffset;
        int stackTopOffset;

        public TextWriter StdOut { get; private set; }

        void Run()
        {
            byte ReadByte() => executingChunk.Code[ipOffset++];
            ushort ReadShort() { ipOffset += 2; return (ushort)(executingChunk.Code[ipOffset - 2] << 8 | executingChunk.Code[ipOffset - 1]); };
            Value ReadConstant() => executingChunk.Constants[ReadByte()];

            void BinaryOp(Func<decimal, decimal, Value> op)
            {
                if (!Peek(0).IsNumber() || !Peek(1).IsNumber())
                    throw new RuntimeExecutionException("Operands must be numbers");
                var b = Pop().AsNumber();
                var a = Pop().AsNumber();
                var result = op(a, b);
                Push(result);
            }

            static bool IsFalsey(Value value) => (value.IsBool() && !value.AsBool()) || (value.IsNumber() && value.AsNumber() == 0M);

            while (true)
            {
                var instruction = (OpCode)ReadByte();
                switch (instruction)
                {
                    case OpCode.Nop:
                        break;
                    case OpCode.Constant:
                        {
                            var constant = ReadConstant();
                            Push(constant);
                        }
                        break;
                    case OpCode.True:
                        Push(Values.Bool(true));
                        break;
                    case OpCode.False:
                        Push(Values.Bool(false));
                        break;
                    case OpCode.Pop:
                        Pop();
                        break;
                    case OpCode.PopN:
                        PopN(ReadByte());
                        break;
                    case OpCode.LoadGlobal:
                        {
                            var name = ReadConstant().AsString();
                            if (!globals.TryGetValue(name, out var value))
                                throw new RuntimeExecutionException($"Undefined variable '{name}'");
                            Push(value);
                        }
                        break;
                    case OpCode.LoadLocal:
                        {
                            var slot = ReadByte();
                            Push(stack[slot]);
                        }
                        break;
                    case OpCode.AssignGlobal:
                        {
                            var name = ReadConstant().AsString();
                            if (!globals.ContainsKey(name))
                                throw new RuntimeExecutionException($"Undefined variable '{name}'");
                            globals[name] = Peek();
                        }
                        break;
                    case OpCode.AssignLocal:
                        {
                            var slot = ReadByte();
                            stack[slot] = Peek();
                        }
                        break;
                    case OpCode.DefineGlobal:
                        {
                            var globalName = ReadConstant().AsString();
                            globals[globalName] = Peek();
                            Pop();
                        }
                        break;
                    case OpCode.Equal:
                        {
                            var a = Pop();
                            var b = Pop();
                            var equals = a.EqualsValue(b);
                            Push(Values.Bool(equals));
                        }
                        break;
                    case OpCode.Greater:
                        BinaryOp((a, b) => Values.Bool(a > b));
                        break;
                    case OpCode.Less:
                        BinaryOp((a, b) => Values.Bool(a < b));
                        break;
                    case OpCode.Add:
                        BinaryOp((a, b) => Values.Number(a + b));
                        break;
                    case OpCode.Subtract:
                        BinaryOp((a, b) => Values.Number(a - b));
                        break;
                    case OpCode.Multiply:
                        BinaryOp((a, b) => Values.Number(a * b));
                        break;
                    case OpCode.Divide:
                        BinaryOp((a, b) => Values.Number(a / b));
                        break;
                    case OpCode.Exp:
                        BinaryOp((a, b) => Values.Number((decimal)Math.Pow((double)a, (double)b)));
                        break;
                    case OpCode.Modulo:
                        BinaryOp((a, b) => Values.Number(decimal.Remainder(a, b)));
                        break;
                    case OpCode.Not:
                        {
                            var falsey = IsFalsey(Pop());
                            Push(Values.Bool(falsey));
                        }
                        break;
                    case OpCode.Negate:
                        {
                            if (!Peek(0).IsNumber())
                                throw new RuntimeExecutionException("Operand must be a number");
                            Push(Values.Number(-Pop().AsNumber()));
                        }
                        break;
                    case OpCode.Print:
                        StdOut.WriteLine(Pop().ToString());
                        break;
                    case OpCode.Jump:
                        {
                            var offset = ReadShort();
                            ipOffset += offset;
                        }
                        break;
                    case OpCode.JumpIfFalse:
                        {
                            var offset = ReadShort();
                            if (IsFalsey(Peek()))
                                ipOffset += offset;
                        }
                        break;
                    case OpCode.Loop:
                        {
                            var offset = ReadShort();
                            ipOffset -= offset;
                        }
                        break;
                    case OpCode.Return:
                        return;

                    default:
                        throw new RuntimeExecutionException($"Unknown instruction value '{instruction}' at ip offset {ipOffset}");
                }
            }
        }

        public void Interpret(Chunk chunk)
        {
            if (chunk is null)
                throw new ArgumentNullException(nameof(chunk));

            executingChunk = chunk;
            ipOffset = 0;

            Run();

            executingChunk = null;
            ipOffset = 0;
        }

        public void Push(Value value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            stack[stackTopOffset++] = value;
        }

        public Value Pop()
        {
            stackTopOffset--;
            if (stackTopOffset < 0)
                throw new RuntimeExecutionException("Attempt to pop off of empty stack");

            return stack[stackTopOffset];
        }

        public void PopN(int count)
        {
            stackTopOffset -= count;
            if (stackTopOffset < 0)
                throw new RuntimeExecutionException("Attempt to pop off of empty stack");
        }

        public Value Peek(int distance = 0)
        {
            var index = stackTopOffset - 1 - distance;
            if (index < 0)
                throw new RuntimeExecutionException("Attempt to peek beyond end of stack");

            return stack[index];
        }

        public void SetStdOut(TextWriter writer) => StdOut = writer ?? throw new ArgumentNullException(nameof(writer));

        public VirtualMachine()
        {
            stack = new Value[MaxStackSize];
            globals = new Dictionary<string, Value>(8);
            SetStdOut(Console.Out);
        }
    }
}
