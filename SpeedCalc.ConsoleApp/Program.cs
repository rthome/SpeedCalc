using SpeedCalc.Core.Runtime;

using System;

namespace SpeedCalc.ConsoleApp
{
    class Program
    {
        static void Main()
        {
            var vm = new VirtualMachine();

            while (true)
            {
                var expression = Console.ReadLine();

                var function = Parser.Compile(expression);

                vm.Interpret(function.Chunk);

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
