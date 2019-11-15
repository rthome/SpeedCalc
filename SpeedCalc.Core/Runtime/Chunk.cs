using System;

namespace SpeedCalc.Core.Runtime
{
    public enum OpCode : byte
    {
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

        public RuntimeArray<IValue> Constants { get; } = new RuntimeArray<IValue>();

        public void Write(OpCode opcode, int line) => Write((byte)opcode, line);

        public void Write(byte codeByte, int line)
        {
            Code.Write(codeByte);
            Lines.Write(line);
        }

        public int AddConstant(IValue constantValue)
        {
            if (constantValue is null)
                throw new ArgumentNullException(nameof(constantValue));

            Constants.Write(constantValue);
            return Constants.Count - 1;
        }
    }
}
