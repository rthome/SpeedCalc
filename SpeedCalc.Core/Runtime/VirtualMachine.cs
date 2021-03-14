using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
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
        sealed class CallFrame
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

        public TextWriter StdOut { get; private set; }

        public TextWriter ErrOut { get; private set; }

        public long InstructionCounter { get; private set; }

        int FrameCount { get; set; } = 0;

        int StackPointer { get; set; } = 0;

        CallFrame Frame { get; set; }

        Chunk CurrentChunk => Frame.Function.Chunk;

        RuntimeArray<byte> Code => CurrentChunk.Code;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsFalsey(Value value) => (value.IsBool() && !value.AsBool()) || (value.IsNumber() && value.AsNumber() == 0M);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        byte ReadByte() => Code[Frame.IP++];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        ushort ReadShort()
        {
            Frame.IP += 2;
            return (ushort)(Code[Frame.IP - 2] << 8 | Code[Frame.IP - 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Value ReadConstant() => CurrentChunk.Constants[ReadByte()];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            Frame = frames[FrameCount - 1];

            while (true)
            {
#if TRACE
                System.Diagnostics.Debug.WriteLine(CurrentChunk.DisassembleInstruction(Frame.IP));
#endif

                InstructionCounter++;
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
                        Pop(ReadByte());
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
                            Push(stack[Frame.StackBase + slot]);
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
                            stack[Frame.StackBase + slot] = Peek();
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
                            Frame.IP += offset;
                        }
                        break;
                    case OpCode.JumpIfFalse:
                        {
                            var offset = ReadShort();
                            if (IsFalsey(Peek()))
                                Frame.IP += offset;
                        }
                        break;
                    case OpCode.Loop:
                        {
                            var offset = ReadShort();
                            Frame.IP -= offset;
                        }
                        break;
                    case OpCode.Call:
                        {
                            var argCount = ReadByte();
                            CallValue(Peek(argCount), argCount);
                            Frame = frames[FrameCount - 1];
                        }
                        break;
                    case OpCode.Return:
                        {
                            var result = Pop();
                            FrameCount--;
                            if (FrameCount == 0)
                            {
                                Pop();
                                return InterpretResult.Success;
                            }

                            StackPointer = Frame.StackBase;
                            Push(result);

                            Frame = frames[FrameCount - 1];
                        }
                        break;

                    default:
                        throw new RuntimeExecutionException($"Unknown instruction value '{instruction}' at ip offset {Frame.IP,4:D4}", CreateStackTrace());
                }
            }
        }

        void Call(Function function, int argCount)
        {
            if (function.Arity != argCount)
                throw new RuntimeExecutionException($"Function {function} expects {function.Arity} arguments but received {argCount}", CreateStackTrace());
            if (FrameCount == MaxCallFrameDepth)
                throw new RuntimeExecutionException("Stack overflow", CreateStackTrace());

            frames[FrameCount++] = new CallFrame
            {
                Function = function,
                IP = 0,
                StackBase = StackPointer - argCount - 1,
            };
        }

        void CallValue(Value callee, int argCount)
        {
            if (callee.IsFunction())
                Call(callee.AsFunction(), argCount);
            else if (callee.IsNativeFunction())
            {
                var nativeDelegate = callee.AsNativeFunction();
                var args = stack[(StackPointer - argCount)..StackPointer];

                var result = nativeDelegate(args);
                if (result is null)
                    throw new RuntimeExecutionException("Native function call returned null value", CreateStackTrace());

                StackPointer -= argCount + 1;
                Push(result);
            }
            else
                throw new RuntimeExecutionException($"Can only call functions - received '{callee}' instead", CreateStackTrace());
        }

        void DefineBuiltinNatives()
        {
            var rng = new Random();

            Value RandomFunction(Value[] _) => Values.Number((decimal)rng.NextDouble());
            Value ClockFunction(Value[] _) => Values.Number((decimal)(DateTime.UtcNow - DateTime.UnixEpoch).TotalSeconds);

            DefineNativeFunction("random", RandomFunction);
            DefineNativeFunction("clock", ClockFunction);
        }

        public void DefineNativeFunction(string functionName, Values.NativeFuncDelegate nativeDelegate)
        {
            if (string.IsNullOrWhiteSpace(functionName))
                throw new ArgumentException($"'{nameof(functionName)}' cannot be null or whitespace.", nameof(functionName));
            if (nativeDelegate is null)
                throw new ArgumentNullException(nameof(nativeDelegate));

            globals[functionName] = Values.NativeFunction(nativeDelegate);
        }

        public string CreateStackTrace()
        {
            var sb = new StringBuilder();
            for (int i = FrameCount - 1; i >= 0; i--)
            {
                var frame = frames[i];
                var line = frame.Function.Chunk.Lines[frame.IP];
                sb.AppendLine($"[line {line}] in {frame.Function}");
            }

            return sb.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Push(Value value)
        {
            stack[StackPointer++] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Pop(int count = 1)
        {
            StackPointer -= count;
            if (StackPointer < 0)
                throw new RuntimeExecutionException("Attempt to pop off of empty stack", CreateStackTrace());

            return stack[StackPointer];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Value Peek(int distance = 0)
        {
            var index = StackPointer - 1 - distance;
            if (index < 0)
                throw new RuntimeExecutionException("Attempt to peek beyond end of stack", CreateStackTrace());

            return stack[index];
        }

        public InterpretResult Interpret(Function function)
        {
            if (function is null)
                throw new ArgumentNullException(nameof(function));

            var functionValue = Values.Function(function);
            Push(functionValue);
            CallValue(functionValue, 0);

            InstructionCounter = 0;

            try
            {
                return Run();
            }
            catch (RuntimeException exc)
            {
                var errorFrame = frames[FrameCount - 1];
                var errorInstr = errorFrame.IP;
                var errorLine = errorFrame.Function.Chunk.Lines[errorInstr];
                var disassembledInstr = errorFrame.Function.Chunk.DisassembleInstruction(errorInstr);
                ErrOut.WriteLine($"[line {errorLine}] Error in {errorFrame.Function} at {disassembledInstr}");
                if (!string.IsNullOrEmpty(exc.Message))
                    ErrOut.WriteLine($" -> {exc.Message}");
                if (!string.IsNullOrEmpty(exc.StackTrace))
                {
                    ErrOut.WriteLine("Stack Trace:");
                    ErrOut.WriteLine(exc.VMStackTrace);
                }

                return InterpretResult.RuntimeError;
            }
        }

        public InterpretResult Interpret(string source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            var parser = new Parser(ErrOut);
            var function = parser.Compile(source);
            if (function is null)
                return InterpretResult.CompileError;

            return Interpret(function);
        }

        public VirtualMachine() : this(Console.Out, Console.Error) { }

        public VirtualMachine(TextWriter standardOutput, TextWriter errorOutput)
        {
            StdOut = standardOutput ?? throw new ArgumentNullException(nameof(standardOutput));
            ErrOut = errorOutput ?? throw new ArgumentNullException(nameof(errorOutput));

            DefineBuiltinNatives();
        }
    }
}
