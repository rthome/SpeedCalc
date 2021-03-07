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
                vm.Interpret(expression);

                // TODO: Add results printing back in later
            }
        }
    }
}
