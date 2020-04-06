using SpeedCalc.Core.Runtime;

using System;

namespace SpeedCalc.ConsoleApp
{
    class Program
    {
        static void Main()
        {
            var vm = new VirtualMachine();
            var chunk = new Chunk();

            while (true)
            {
                var expression = Console.ReadLine();

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
