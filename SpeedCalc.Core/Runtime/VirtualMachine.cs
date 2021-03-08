using System;
using System.Collections.Generic;
using System.IO;

namespace SpeedCalc.Core.Runtime
{
    public enum InterpretResult
    {
        Success,
        CompileError,
        RuntimeError,
    }

    public sealed class VirtualMachine
    {
        struct CallFrame
        {
            public Function Function;

            public int IP;

            public int StackBase;
        }

        const int MaxCallFrameDepth = 256;
        const int MaxStackSize = MaxCallFrameDepth * 64;

        readonly CallFrame[] frames = new CallFrame[MaxCallFrameDepth];
        readonly Value[] stack = new Value[MaxStackSize];
        readonly Dictionary<string, Value> globals = new Dictionary<string, Value>();

        int frameCount = 0;
        int stackTopOffset = 0;

        public TextWriter StdOut { get; private set; }

        InterpretResult Run()
        {
            CallFrame frame = frames[frameCount - 1];

            var chunk = frame.Function.Chunk;
            var code = frame.Function.Chunk.Code;

            byte ReadByte() => code[frame.IP++];
            ushort ReadShort() { frame.IP += 2; return (ushort)(code[frame.IP - 2] << 8 | code[frame.IP - 1]); };
            Value ReadConstant() => chunk.Constants[ReadByte()];

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
#if TRACE
                System.Diagnostics.Debug.WriteLine(chunk.DisassembleInstruction(frame.IP));
#endif

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
                            Push(stack[frame.StackBase + slot]);
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
                            stack[frame.StackBase + slot] = Peek();
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
                            frame.IP += offset;
                        }
                        break;
                    case OpCode.JumpIfFalse:
                        {
                            var offset = ReadShort();
                            if (IsFalsey(Peek()))
                                frame.IP += offset;
                        }
                        break;
                    case OpCode.Loop:
                        {
                            var offset = ReadShort();
                            frame.IP -= offset;
                        }
                        break;
                    case OpCode.Return:
                        return InterpretResult.Success;

                    default:
                        throw new RuntimeExecutionException($"Unknown instruction value '{instruction}' at ip offset {frame.IP,4:D4}");
                }
            }
        }

        public InterpretResult Interpret(string source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var parser = new Parser();
            var function = parser.Compile(source);
            if (function is null)
                return InterpretResult.CompileError;

            Push(Values.Function(function));
            ref var frame = ref frames[frameCount++];
            frame.Function = function;
            frame.IP = 0;
            frame.StackBase = 0;

            try
            {
                return Run();
            }
            catch (RuntimeException exc)
            {
                var errorFrame = frames[frameCount - 1];
                var errorInstr = errorFrame.IP;
                var errorLine = errorFrame.Function.Chunk.Lines[errorInstr];
                var disassembledInstr = errorFrame.Function.Chunk.DisassembleInstruction(errorInstr);
                StdOut.WriteLine($"[line {errorLine}] Error in {errorFrame.Function} at {disassembledInstr}");
                if (!string.IsNullOrEmpty(exc.Message))
                    StdOut.WriteLine($"    {exc.Message}");
                return InterpretResult.RuntimeError;
            }
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
            SetStdOut(Console.Out);
        }
    }
}
