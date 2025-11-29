using BenchmarkDotNet.Running;

namespace Serilog.Sinks.SqlServer.Benchmark;

internal class Program
{
    static void Main(string[] args)
    {
        var _ = BenchmarkRunner.Run(typeof(Program).Assembly);
    }
}
