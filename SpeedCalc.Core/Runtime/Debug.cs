using System.Text;

namespace SpeedCalc.Core.Runtime
{
    public static class Debug
    {
        static (int newOffset, string instruction) Disassemble(Chunk chunk, int offset, bool alwaysWriteLine = false)
        {
            var sb = new StringBuilder();
            sb.Append($"{offset,4:D4} ");
            if (!alwaysWriteLine && offset > 0 && chunk.Lines[offset] == chunk.Lines[offset - 1])
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
                case OpCode.Call:
                    var slot = chunk.Code[offset + 1];
                    sb.Append($"{instr} {slot,4:D4}");
                    return (offset + 2, sb.ToString());

                case OpCode.Jump:
                case OpCode.JumpIfFalse:
                case OpCode.Loop:
                    var jump = (ushort)(chunk.Code[offset + 1] << 8);
                    jump |= chunk.Code[offset + 2];
                    if (instr == OpCode.Loop)
                        sb.Append($"{instr} {offset,4:D4} -> {(offset + 3) - jump,4:D4}");
                    else
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
                case OpCode.Modulo:
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
            return Disassemble(chunk, offset, alwaysWriteLine: true).instruction;
        }

        public static string DisassembleChunk(this Chunk chunk)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < chunk.Code.Count;)
            {
                var (newOffset, instruction) = Disassemble(chunk, i);
                i = newOffset;
                sb.AppendLine(instruction);
            }
            return sb.ToString();
        }

        public static string DisassembleFunction(this Function function)
        {
            var disassembledChunk = function.Chunk.DisassembleChunk();
            var prefix = $"---- {function} ----";

            var sb = new StringBuilder();
            sb.AppendLine(prefix);
            sb.Append(disassembledChunk);
            return sb.ToString();
        }
    }
}
