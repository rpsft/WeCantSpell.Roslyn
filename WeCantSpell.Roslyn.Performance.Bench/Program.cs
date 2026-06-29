using System.Globalization;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using Perfolizer.Horology;
using Perfolizer.Metrology;

namespace WeCantSpell.Roslyn.Performance.Bench
{
    internal static class Program
    {
        private static void Main()
        {
            var config = ManualConfig
                .CreateMinimumViable()
                .AddDiagnoser(new MemoryDiagnoser(new MemoryDiagnoserConfig()))
                .AddLogger(ConsoleLogger.Default)
                .AddColumn(
                    TargetMethodColumn.Method,
                    StatisticColumn.Median,
                    StatisticColumn.StdDev,
                    StatisticColumn.Error
                )
                .WithSummaryStyle(
                    new SummaryStyle(CultureInfo.InvariantCulture, true, SizeUnit.MB, TimeUnit.Millisecond)
                );
            BenchmarkRunner.Run<ThisSolutionPerfSpec>(config);
        }
    }
}
