using System.Diagnostics;
using System.Numerics;
using PhysicsEngine.Dynamics;
using PhysicsEngine.Serialization;

namespace PhysicsBenchmark;

public static class BenchmarkRunner
{
    private const int Seed = 42;
    private const int WarmupRuns = 1;
    private const int MeasuredRuns = 20;
    private const int TotalRuns = WarmupRuns + MeasuredRuns;
    private const int StepsPerRun = 500;
    private const string ResultsDir = "results";

    public static void RunAll()
    {
        Directory.CreateDirectory(ResultsDir);

        var coreCount = Environment.ProcessorCount;
        Console.WriteLine($"CPU cores: {coreCount}");
        Console.WriteLine($"Iterations per measurement: {MeasuredRuns} (+ {WarmupRuns} warmup)");
        Console.WriteLine();

        RunSeries1(coreCount);
        RunSeries2();
        RunSeries3();
    }

    private static void RunSeries1(int threadCount)
    {
        Console.WriteLine("Series 1: Body Count Scaling");
        Console.WriteLine($"Thread count (fixed): {threadCount}");

        int[] bodyCounts = [100, 250, 500, 1000, 2500, 5000];
        var rows = new List<CsvRow>();

        PrintHeader();

        foreach (var bodyCount in bodyCounts)
        {
            var config = MakeScenario(bodyCount);
            var seqMs = Measure(config, ParallelStrategy.Sequential, 1);
            var parMs = Measure(config, ParallelStrategy.ParallelFor, threadCount);
            var speedup = seqMs / parMs;

            PrintRow(bodyCount, seqMs, parMs, speedup);
            rows.Add(new CsvRow(bodyCount, seqMs, parMs, speedup));
        }

        WriteCsv(Path.Combine(ResultsDir, "series1.csv"), "bodyCount", rows);
        Console.WriteLine($"Saved to {ResultsDir}/series1.csv");
        Console.WriteLine();
    }

    private static void RunSeries2()
    {
        Console.WriteLine("Series 2: Thread Count Impact");
        const int bodyCount = 5000;
        var config = MakeScenario(bodyCount);

        var seqMs = Measure(config, ParallelStrategy.Sequential, 1);
        Console.WriteLine($"Sequential baseline ({bodyCount} bodies): {seqMs:F2} ms");

        int[] threadCounts = [2, 4, 8, 16];
        var rows = new List<CsvRow>();

        PrintHeader("Threads");

        foreach (var tc in threadCounts)
        {
            var parMs = Measure(config, ParallelStrategy.ParallelFor, tc);
            var speedup = seqMs / parMs;

            PrintRow(tc, seqMs, parMs, speedup);
            rows.Add(new CsvRow(tc, seqMs, parMs, speedup));
        }

        WriteCsv(Path.Combine(ResultsDir, "series2.csv"), "threadCount", rows);
        Console.WriteLine($"Saved to {ResultsDir}/series2.csv");
        Console.WriteLine();
    }

    private static void RunSeries3()
    {
        Console.WriteLine("Series 3: Parallelization Strategy Comparison");
        const int bodyCount = 5000;
        var threadCount = Environment.ProcessorCount;
        var config = MakeScenario(bodyCount);

        var seqMs = Measure(config, ParallelStrategy.Sequential, 1);
        var parForMs = Measure(config, ParallelStrategy.ParallelFor, threadCount);
        var taskMs = Measure(config, ParallelStrategy.TaskBased, threadCount);
        var poolMs = Measure(config, ParallelStrategy.ThreadPool, threadCount);

        Console.WriteLine($"{"Strategy",-16} {"Time (ms)",14} {"Speedup",10}");
        Console.WriteLine($"{"",-16} {"",14} {"",10}".Replace(' ', '-'));
        Console.WriteLine($"{"Sequential",-16} {seqMs,14:F2} {"1.00x",10}");
        Console.WriteLine($"{"Parallel.For",-16} {parForMs,14:F2} {seqMs / parForMs,9:F2}x");
        Console.WriteLine($"{"Task.Run",-16} {taskMs,14:F2} {seqMs / taskMs,9:F2}x");
        Console.WriteLine($"{"ThreadPool",-16} {poolMs,14:F2} {seqMs / poolMs,9:F2}x");

        var path = Path.Combine(ResultsDir, "series3.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("strategy,sequentialMs,strategyMs,speedup");
        writer.WriteLine($"Sequential,{seqMs:F4},{seqMs:F4},{1.0:F4}");
        writer.WriteLine($"ParallelFor,{seqMs:F4},{parForMs:F4},{seqMs / parForMs:F4}");
        writer.WriteLine($"TaskBased,{seqMs:F4},{taskMs:F4},{seqMs / taskMs:F4}");
        writer.WriteLine($"ThreadPool,{seqMs:F4},{poolMs:F4},{seqMs / poolMs:F4}");

        Console.WriteLine($"Saved to {ResultsDir}/series3.csv");
        Console.WriteLine();
    }

    private static ScenarioConfig MakeScenario(int bodyCount)
    {
        var boxHalf = MathF.Cbrt(bodyCount * 8f);
        return ScenarioBuilder.GenerateRandom(Seed, bodyCount, new Vector3(boxHalf, boxHalf, boxHalf));
    }

    private static double Measure(ScenarioConfig config, ParallelStrategy strategy, int threadCount)
    {
        var times = new double[TotalRuns];
        var sw = new Stopwatch();

        for (var run = 0; run < TotalRuns; run++)
        {
            var world = ScenarioBuilder.BuildWorld(config);

            sw.Restart();
            for (var step = 0; step < StepsPerRun; step++)
                world.Simulate(config.TimeStep, strategy, threadCount);
            sw.Stop();

            times[run] = sw.Elapsed.TotalMilliseconds;
        }

        double sum = 0;
        for (int i = WarmupRuns; i < TotalRuns; i++)
            sum += times[i];
        return sum / MeasuredRuns;
    }

    private static void PrintHeader(string paramName = "Bodies")
    {
        Console.WriteLine($"{paramName,-10} {"Seq (ms)",14} {"Par (ms)",14} {"Speedup",10}");
        Console.WriteLine($"{"",-10} {"",14} {"",14} {"",10}".Replace(' ', '-'));
    }

    private static void PrintRow(int param, double seqMs, double parMs, double speedup)
    {
        Console.WriteLine($"{param,-10} {seqMs,14:F2} {parMs,14:F2} {speedup,9:F2}x");
    }

    private record CsvRow(int Parameter, double SeqMs, double ParMs, double Speedup);

    private static void WriteCsv(string path, string paramName, List<CsvRow> rows)
    {
        using var writer = new StreamWriter(path);
        writer.WriteLine($"{paramName},sequentialMs,parallelMs,speedup");
        foreach (var r in rows)
            writer.WriteLine($"{r.Parameter},{r.SeqMs:F4},{r.ParMs:F4},{r.Speedup:F4}");
    }
}
