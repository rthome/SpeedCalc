using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
        class CallFrame
        {
            public Function Function { get; set; }

            public int IP { get; set; }

            public int StackBase { get; set; }
        }

        const int MaxCallFrameDepth = 256;
        const int MaxStackSize = MaxCallFrameDepth * 64;

        readonly CallFrame[] frames = new CallFrame[MaxCallFrameDepth];
        readonly Value[] stack = new Value[MaxStackSize];
        readonly Dictionary<string, Value> globals = new Dictionary<string, Value>();

        int frameCount = 0;
        int stackPointer = 0;

        CallFrame frame;

        public TextWriter StdOut { get; private set; }

        Chunk CurrentChunk => frame.Function.Chunk;

        RuntimeArray<byte> Code => CurrentChunk.Code;

        static bool IsFalsey(Value value) => (value.IsBool() && !value.AsBool()) || (value.IsNumber() && value.AsNumber() == 0M);

        byte ReadByte() => Code[frame.IP++];

        ushort ReadShort()
        {
            frame.IP += 2;
            return (ushort)(Code[frame.IP - 2] << 8 | Code[frame.IP - 1]);
        }

        Value ReadConstant() => CurrentChunk.Constants[ReadByte()];

        void BinaryOp(Func<decimal, decimal, Value> op)
        {
            if (!Peek(0).IsNumber() || !Peek(1).IsNumber())
                throw new RuntimeExecutionException("Operands must be numbers", CreateStackTrace());
            var b = Pop().AsNumber();
            var a = Pop().AsNumber();
            var result = op(a, b);
            Push(result);
        }

        InterpretResult Run()
        {
            frame = frames[frameCount - 1];

            while (true)
            {
#if TRACE
                System.Diagnostics.Debug.WriteLine(CurrentChunk.DisassembleInstruction(frame.IP));
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
                                throw new RuntimeExecutionException($"Undefined variable '{name}'", CreateStackTrace());
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
                                throw new RuntimeExecutionException($"Undefined variable '{name}'", CreateStackTrace());
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
                                throw new RuntimeExecutionException("Operand must be a number", CreateStackTrace());
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
                    case OpCode.Call:
                        {
                            var argCount = ReadByte();
                            CallValue(Peek(argCount), argCount);
                            frame = frames[frameCount - 1];
                        }
                        break;
                    case OpCode.Return:
                        {
                            var result = Pop();
                            frameCount--;
                            if (frameCount == 0)
                            {
                                Pop();
                                return InterpretResult.Success;
                            }

                            stackPointer = frame.StackBase;
                            Push(result);

                            frame = frames[frameCount - 1];
                        }
                        break;

                    default:
                        throw new RuntimeExecutionException($"Unknown instruction value '{instruction}' at ip offset {frame.IP,4:D4}", CreateStackTrace());
                }
            }
        }

        void Call(Function function, int argCount)
        {
            if (function.Arity != argCount)
                throw new RuntimeExecutionException($"Function {function} expects {function.Arity} arguments but received {argCount}", CreateStackTrace());
            if (frameCount == MaxCallFrameDepth)
                throw new RuntimeExecutionException("Stack overflow", CreateStackTrace());

            frames[frameCount++] = new CallFrame
            {
                Function = function,
                IP = 0,
                StackBase = stackPointer - argCount - 1,
            };
        }

        void CallValue(Value callee, int argCount)
        {
            if (callee.IsFunction())
                Call(callee.AsFunction(), argCount);
            else
                throw new RuntimeExecutionException($"Can only call functions - received '{callee}' instead", CreateStackTrace());
        }

        public InterpretResult Interpret(string source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var parser = new Parser();
            var function = parser.Compile(source);
            if (function is null)
                return InterpretResult.CompileError;

            var functionValue = Values.Function(function);
            Push(functionValue);
            CallValue(functionValue, 0);

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
                if (!string.IsNullOrEmpty(exc.StackTrace))
                {
                    StdOut.WriteLine("Stack Trace:");
                    StdOut.WriteLine(exc.VMStackTrace);
                }
                return InterpretResult.RuntimeError;
            }
        }

        public string CreateStackTrace()
        {
            var sb = new StringBuilder();
            for (int i = frameCount - 1; i >= 0; i--)
            {
                var frame = frames[i];
                var line = frame.Function.Chunk.Lines[frame.IP];
                sb.AppendLine($"[line {line}] in {frame.Function}");
            }

            return sb.ToString();
        }

        public void Push(Value value)
        {
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            stack[stackPointer++] = value;
        }

        public Value Pop()
        {
            stackPointer--;
            if (stackPointer < 0)
                throw new RuntimeExecutionException("Attempt to pop off of empty stack", CreateStackTrace());

            return stack[stackPointer];
        }

        public void PopN(int count)
        {
            stackPointer -= count;
            if (stackPointer < 0)
                throw new RuntimeExecutionException("Attempt to pop off of empty stack", CreateStackTrace());
        }

        public Value Peek(int distance = 0)
        {
            var index = stackPointer - 1 - distance;
            if (index < 0)
                throw new RuntimeExecutionException("Attempt to peek beyond end of stack", CreateStackTrace());

            return stack[index];
        }

        public void SetStdOut(TextWriter writer) => StdOut = writer ?? throw new ArgumentNullException(nameof(writer));

        public VirtualMachine()
        {
            SetStdOut(Console.Out);
        }
    }
}
