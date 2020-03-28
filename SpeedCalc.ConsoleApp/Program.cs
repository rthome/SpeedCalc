using SpeedCalc.Core.Runtime;

using System;

namespace SpeedCalc.ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var vm = new VirtualMachine();

            while (true)
            {
                var expression = Console.ReadLine();

                var chunk = new Chunk();
                Parser.Compile(expression, chunk);

                vm.Interpret(chunk);

                try
                {
                    var value = vm.Pop();
                    Console.WriteLine($"  = {value}");
                }
                catch (RuntimeExecutionException)
                {
                }
            }
        }
    }
}
