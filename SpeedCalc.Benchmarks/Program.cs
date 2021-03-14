using SpeedCalc.Core.Runtime;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace SpeedCalc.Benchmarks
{
    class Benchmark
    {
        public string Name { get; }

        public string Category { get; }

        public string Code { get; }

        public RunResults Run(int runCount = 3)
        {
            var warmupVM = new VirtualMachine();
            var result = warmupVM.Interpret(Code);
            if (result != InterpretResult.Success)
                throw new Exception("Error while executing benchmark");

            var results = new RunResults(this);
            var stopwatch = new Stopwatch();
            for (int i = 0; i < runCount; i++)
            {
                stopwatch.Start();

                var vm = new VirtualMachine();
                vm.Interpret(Code);

                stopwatch.Stop();
                results.AddRun(stopwatch.Elapsed, vm.InstructionCounter);
                stopwatch.Reset();
            }

            return results;
        }

        public Benchmark(string category, string name, string code)
        {
            Category = category;
            Name = name;
            Code = code;
        }
    }

    class RunResults
    {
        readonly List<TimeSpan> durations = new();

        readonly List<long> instructionCounts = new();

        public Benchmark Benchmark { get; }

        public IEnumerable<TimeSpan> Durations => durations.AsEnumerable();

        public IEnumerable<long> InstructionCounts => instructionCounts.AsEnumerable();

        public void AddRun(TimeSpan duration, long instructionsRun)
        {
            durations.Add(duration);
            instructionCounts.Add(instructionsRun);
        }

        public RunResults(Benchmark benchmark)
        {
            Benchmark = benchmark;
        }
    }

    static class BenchmarkSources
    {
        public const string RecursiveFibonacci = @"
            fn fibonacci(n) {
                if n <= 2:
                    return 1;
                else:
                    return fibonacci(n - 1) + fibonacci(n - 2);
            }
            
            fibonacci(30);";

        public const string NewtonRoot = @"
            fn newton(x, n) {
                var y = x / 2;
                for var i = 0; i < 8; i += 1: {
                    y = ((n-1) * y**n + x) / (n * y**(n-1));
                }
                return y;
            }
            
            for var i = 0; i < 10000; i += 1: {
                newton(58463165, 2);
                newton(8732648, 3);
                newton(216876, 5);
            }";

        public static string IncrementVariable = $"var i = 0; {Enumerable.Repeat("i += 1;", 1000000).Aggregate(new StringBuilder(), (sb, v) => sb.Append(v), sb => sb.ToString())}";
    }
    
    class Program
    {
        const string CategoryMicroBenchmark = "MicroBenchmark";
        const string CategoryCalculations = "Calculations";
        const string CategoryNative = "Native Calls";

        static List<Benchmark> Benchmarks { get; } = new List<Benchmark>
        {
            new Benchmark(CategoryMicroBenchmark, "CountForLoop", "for var i = 0; i < 1000000; i += 1: {}"),
            new Benchmark(CategoryMicroBenchmark, "CountWhileLoop", "var i = 0; while i < 1000000: i+= 1;"),
            new Benchmark(CategoryMicroBenchmark, "FunctionCallGlobal", "fn nop() {} for var i = 0; i < 1000000; i += 1: { nop(); }"),
            new Benchmark(CategoryMicroBenchmark, "FunctionCallLocal", "{ fn nop() {} for var i = 0; i < 1000000; i += 1: { nop(); } }"),
            new Benchmark(CategoryMicroBenchmark, "IncrementVariable", BenchmarkSources.IncrementVariable),

            new Benchmark(CategoryNative, "ClockCalls", "for var i = 0; i < 1000000; i += 1: clock();"),
            new Benchmark(CategoryNative, "RandomCalls", "for var i = 0; i < 1000000; i += 1: random();"),

            new Benchmark(CategoryCalculations, "RecursiveFibonacci30", BenchmarkSources.RecursiveFibonacci),
            new Benchmark(CategoryCalculations, "NewtonRoots", BenchmarkSources.NewtonRoot),
        };

        static void PrintResults(IEnumerable<RunResults> results)
        {
            static string PadCenter(string value, int length) => value.PadLeft((length + value.Length) / 2).PadRight(length);
            static string FormatNumber(double number, int length, string suffix = "")
            {
                FormattableString str = $"{number:F1}";
                return PadCenter(str.ToString(CultureInfo.InvariantCulture) + suffix, length);
            }

            var category = "Category";
            var name = "Name";
            var avgDuration = "Avg Duration";
            var instructions = "Instructions";
            var avgInstrPerMs = "Avg Instr/ms";

            var maxCategoryLength = results.Select(r => r.Benchmark.Category).Max(c => c.Length);
            var maxNameLength = results.Select(r => r.Benchmark.Name).Max(c => c.Length);
            var durationColumnSize = avgDuration.Length;
            var instructionsColumnSize = instructions.Length;
            var instrPerMsColumnSize = avgInstrPerMs.Length;

            category = PadCenter(category, maxCategoryLength);
            name = PadCenter(name, maxNameLength);
            avgDuration = PadCenter(avgDuration, durationColumnSize);
            instructions = PadCenter(instructions, instructionsColumnSize);
            avgInstrPerMs = PadCenter(avgInstrPerMs, instrPerMsColumnSize);

            var separatorLine = $"+{new string('-', category.Length + 2)}+{new string('-', name.Length + 2)}+{new string('-', durationColumnSize + 2)}+{new string('-', instructionsColumnSize + 2)}+{new string('-', instrPerMsColumnSize + 2)}+";
            Console.WriteLine(separatorLine);
            Console.WriteLine($"| {category} | {name} | {avgDuration} | {instructions} | {avgInstrPerMs} |");
            Console.WriteLine(separatorLine);

            foreach (var result in results)
            {
                var durationMs = result.Durations.Average(d => d.TotalMilliseconds);
                var instrs = (long)result.InstructionCounts.Average();
                var instrPerMs = result.InstructionCounts.Average() / durationMs;

                Console.WriteLine($"| {result.Benchmark.Category.PadRight(maxCategoryLength)} | {result.Benchmark.Name.PadRight(maxNameLength)} | {FormatNumber(durationMs, durationColumnSize, "ms")} | {PadCenter(instrs.ToString(), instructionsColumnSize)} | {FormatNumber(instrPerMs, instrPerMsColumnSize)} |");
            }
        }

        static void Main()
        {
            Console.WriteLine("Press any key to run start benchmarks...");
            Console.ReadKey();

            var results = new List<RunResults>();
            foreach (var benchmark in Benchmarks.OrderBy(b => b.Category).ThenBy(b => b.Name))
            {
                var result = benchmark.Run();
                results.Add(result);
            }

            PrintResults(results);

            Console.WriteLine();
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
