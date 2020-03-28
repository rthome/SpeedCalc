using System;

namespace SpeedCalc.Core.Runtime
{
    public sealed class VirtualMachine
    {
        const int MaxStackSize = 256;

        readonly Value[] stack;
        Chunk executingChunk;
        int ipOffset;
        int stackTopOffset;

        void Run()
        {
            byte ReadByte() => executingChunk.Code[ipOffset++];
            Value ReadConstant() => executingChunk.Constants[ReadByte()];

            void BinaryOp(Func<decimal, decimal, Value> op)
            {
                if (!Peek(0).IsNumber() || !Peek(1).IsNumber())
                    throw new RuntimeExecutionException("Operands must be numbers");
                var a = Pop().AsNumber();
                var b = Pop().AsNumber();
                var result = op(a, b);
                Push(result);
            }

            static bool IsFalsey(Value value) => value.IsNil() || (value.IsBool() && !value.AsBool()) || (value.IsNumber() && value.AsNumber() == 0M);

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
                    case OpCode.Nil:
                        Push(Values.Nil());
                        break;
                    case OpCode.True:
                        Push(Values.Bool(true));
                        break;
                    case OpCode.False:
                        Push(Values.Bool(false));
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
                    case OpCode.Not:
                        {
                            var falsey = IsFalsey(Pop());
                            Push(Values.Bool(falsey));
                        }
                        break;
                    case OpCode.Negate:
                        break;
                    case OpCode.Return:
                        return;
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

        public void Interpret(string source)
        {
            throw new NotImplementedException();
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

        public Value Peek(int distance = 0)
        {
            var index = stackTopOffset - 1 - distance;
            if (index < 0)
                throw new RuntimeExecutionException("Attempt to peek beyond end of stack");

            return stack[index];
        }

        public VirtualMachine()
        {
            stack = new Value[MaxStackSize];
        }
    }
}
