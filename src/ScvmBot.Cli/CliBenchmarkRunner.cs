using ScvmBot.Modules;
using System.Diagnostics;

namespace ScvmBot.Cli;

/// <summary>
/// Executes benchmark mode (<c>--quiet</c>) for a CLI generation command,
/// handling iteration timing and console output. Extracted from Program.cs
/// so that the entrypoint remains a thin composition layer.
/// </summary>
internal static class CliBenchmarkRunner
{
    internal static async Task RunAsync(
        IGameModule module,
        string subCommandName,
        IReadOnlyDictionary<string, object?> options,
        CliOptions cliOpts,
        CancellationToken ct)
    {
        if (cliOpts.Detailed)
        {
            var ticksPerGeneration = new long[cliOpts.Count];
            var sw = new Stopwatch();
            var startTime = DateTimeOffset.Now;
            var totalSw = Stopwatch.StartNew();

            for (var n = 0; n < cliOpts.Count; n++)
            {
                sw.Restart();
                await module.HandleGenerateCommandAsync(subCommandName, options, ct);
                sw.Stop();
                ticksPerGeneration[n] = sw.ElapsedTicks;
            }

            totalSw.Stop();
            var endTime = DateTimeOffset.Now;

            Array.Sort(ticksPerGeneration);
            var tickFreq = (double)Stopwatch.Frequency;
            var minMs = ticksPerGeneration[0] / tickFreq * 1000;
            var maxMs = ticksPerGeneration[cliOpts.Count - 1] / tickFreq * 1000;
            var medianMs = ticksPerGeneration[cliOpts.Count / 2] / tickFreq * 1000;
            var avgMs = ticksPerGeneration.Average() / tickFreq * 1000;
            var p95Ms = ticksPerGeneration[(int)(cliOpts.Count * 0.95)] / tickFreq * 1000;
            var p99Ms = ticksPerGeneration[(int)(cliOpts.Count * 0.99)] / tickFreq * 1000;

            Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
            Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
            Console.WriteLine($"  Count:     {cliOpts.Count:N0}");
            Console.WriteLine($"  Elapsed:   {totalSw.Elapsed}");
            Console.WriteLine();
            Console.WriteLine("  Per-generation timing:");
            Console.WriteLine($"    Min:     {minMs:F4} ms");
            Console.WriteLine($"    Max:     {maxMs:F4} ms");
            Console.WriteLine($"    Avg:     {avgMs:F4} ms");
            Console.WriteLine($"    Median:  {medianMs:F4} ms");
            Console.WriteLine($"    P95:     {p95Ms:F4} ms");
            Console.WriteLine($"    P99:     {p99Ms:F4} ms");
        }
        else
        {
            var startTime = DateTimeOffset.Now;
            var sw = Stopwatch.StartNew();

            for (var n = 0; n < cliOpts.Count; n++)
                await module.HandleGenerateCommandAsync(subCommandName, options, ct);

            sw.Stop();
            var endTime = DateTimeOffset.Now;

            Console.WriteLine($"  Started:   {startTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
            Console.WriteLine($"  Finished:  {endTime:yyyy-MM-dd HH:mm:ss.fff zzz}");
            Console.WriteLine($"  Count:     {cliOpts.Count:N0}");
            Console.WriteLine($"  Elapsed:   {sw.Elapsed}");
        }
    }
}
