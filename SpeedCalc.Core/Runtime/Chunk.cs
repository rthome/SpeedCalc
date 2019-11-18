using System;

namespace SpeedCalc.Core.Runtime
{
    public enum OpCode : byte
    {
        Nop,
        Constant,
        Nil,
        True,
        False,
        Equal,
        Greater,
        Less,
        Add,
        Subtract,
        Multiply,
        Divide,
        Not,
        Negate,
        Return,
    };

    public sealed class Chunk
    {
        public RuntimeArray<byte> Code { get; } = new RuntimeArray<byte>();

        public RuntimeArray<int> Lines { get; } = new RuntimeArray<int>();

        public RuntimeArray<Value> Constants { get; } = new RuntimeArray<Value>();

        public void Write(OpCode opcode, int line) => Write((byte)opcode, line);

        public void Write(byte codeByte, int line)
        {
            Code.Write(codeByte);
            Lines.Write(line);
        }

        public void Write(OpCode opcode, byte arg, int line)
        {
            Code.Write((byte)opcode);
            Lines.Write(line);
            Code.Write(arg);
            Lines.Write(line);
        }

        public int AddConstant(Value constantValue)
        {
            if (constantValue is null)
                throw new ArgumentNullException(nameof(constantValue));

            Constants.Write(constantValue);
            return Constants.Count - 1;
        }
    }
}
