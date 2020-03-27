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
                        break;
                    case OpCode.True:
                        Push(Values.Bool(true));
                        break;
                    case OpCode.False:
                        Push(Values.Bool(false));
                        break;
                    case OpCode.Equal:
                        break;
                    case OpCode.Greater:
                        break;
                    case OpCode.Less:
                        break;
                    case OpCode.Add:
                        break;
                    case OpCode.Subtract:
                        break;
                    case OpCode.Multiply:
                        break;
                    case OpCode.Divide:
                        break;
                    case OpCode.Not:
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

        public VirtualMachine()
        {
            stack = new Value[MaxStackSize];
        }
    }
}
