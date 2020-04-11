using System.Collections.Generic;
using System.Text;

namespace SpeedCalc.Core.Runtime
{
    public static class Debug
    {
        static (int newOffset, string instruction) Disassemble(Chunk chunk, int offset)
        {
            var sb = new StringBuilder();
            sb.Append($"{offset,4:D4} ");
            if (offset > 0 && chunk.Lines[offset] == chunk.Lines[offset - 1])
                sb.Append("   | ");
            else
                sb.Append($"{chunk.Lines[offset],4:D4} ");

            var instr = (OpCode)chunk.Code[offset];
            switch (instr)
            {
                case OpCode.Constant:
                case OpCode.LoadGlobal:
                case OpCode.AssignGlobal:
                case OpCode.DefineGlobal:
                    var constant = chunk.Code[offset + 1];
                    sb.Append($"{instr} {constant,4:D4} {chunk.Constants[constant]}");
                    return (offset + 2, sb.ToString());

                case OpCode.PopN:
                case OpCode.LoadLocal:
                case OpCode.AssignLocal:
                    var slot = chunk.Code[offset + 1];
                    sb.Append($"{instr} {slot,4:D4}");
                    return (offset + 2, sb.ToString());

                case OpCode.Jump:
                case OpCode.JumpIfFalse:
                    var jump = (ushort)(chunk.Code[offset + 1] << 8);
                    jump |= chunk.Code[offset + 2];
                    sb.Append($"{instr} {offset,4:D4} -> {(offset + 3) + jump,4:D4}");
                    return (offset + 3, sb.ToString());

                case OpCode.Nop:
                case OpCode.True:
                case OpCode.False:
                case OpCode.Pop:
                case OpCode.Equal:
                case OpCode.Greater:
                case OpCode.Less:
                case OpCode.Add:
                case OpCode.Subtract:
                case OpCode.Multiply:
                case OpCode.Divide:
                case OpCode.Exp:
                case OpCode.Not:
                case OpCode.Negate:
                case OpCode.Print:
                case OpCode.Return:
                    sb.Append(instr.ToString());
                    return (offset + 1, sb.ToString());

                default:
                    sb.Append($"Unknown opcode {instr}");
                    return (offset + 1, sb.ToString());
            }
        }

        public static string DisassembleInstruction(this Chunk chunk, int offset)
        {
            return Disassemble(chunk, offset).instruction;
        }

        public static IEnumerable<string> DisassembleChunk(this Chunk chunk)
        {
            var disassembledInstructions = new List<string>();
            for (int i = 0; i < chunk.Code.Count;)
            {
                var (newOffset, instruction) = Disassemble(chunk, i);
                i = newOffset;
                disassembledInstructions.Add(instruction);
            }
            return disassembledInstructions;
        }
    }
}
