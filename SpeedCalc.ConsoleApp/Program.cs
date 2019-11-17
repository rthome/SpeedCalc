using SpeedCalc.Core.Runtime;

using System;

namespace SpeedCalc.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var chunk = new Chunk();
            chunk.AddConstant(Values.Number(1m));
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Constant, 0, 1);
            chunk.Write(OpCode.Add, 1);
            chunk.Write(OpCode.Return, 1);

            foreach (var instruction in chunk.DisassembleChunk())
                Console.WriteLine(instruction);
        }
    }
}
