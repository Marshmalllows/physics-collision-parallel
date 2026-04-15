using System.Diagnostics;
using System.Numerics;
using PhysicsEngine.Core;
using PhysicsEngine.Dynamics;

namespace PhysicsBenchmark;

public static class BenchmarkRunner
{
    private const int Seed = 42;
    private const int WarmupRuns = 1;
    private const int MeasuredRuns = 20;
    private const int TotalRuns = WarmupRuns + MeasuredRuns;
    private const int StepsPerRun = 500;
    private const string ResultsDir = "results";

    private static readonly Vector3[] TetraVerts =
    [
        new(0f, 0.612f, 0f),
        new(-0.5f, -0.204f, 0.289f),
        new(0.5f, -0.204f, 0.289f),
        new(0f, -0.204f, -0.577f)
    ];

    public static void RunAll()
    {
        Directory.CreateDirectory(ResultsDir);
        var coreCount = Environment.ProcessorCount;
        RunSeries1(coreCount);
        RunSeries2();
        RunSeries3();
    }

    private static void PrintBenchmarkHeader()
    {
        Directory.CreateDirectory(ResultsDir);
        Console.WriteLine("Benchmarks (ConvexMesh tetrahedra)");
        Console.WriteLine("----------------------------------");
        Console.WriteLine($"CPU cores: {Environment.ProcessorCount}, runs: {MeasuredRuns} (+{WarmupRuns} warmup), steps/run: {StepsPerRun}");
        Console.WriteLine();
    }

    public static void RunSeries1(int threadCount = 0)
    {
        if (threadCount <= 0) threadCount = Environment.ProcessorCount;
        PrintBenchmarkHeader();
        Console.WriteLine("Series 1: Body count scaling");
        Console.WriteLine($"Threads: {threadCount}");

        int[] bodyCounts = [50, 100, 200, 400, 800];
        var rows = new List<CsvRow>();

        PrintHeader();

        foreach (var bodyCount in bodyCounts)
        {
            var world = MakeConvexWorld(bodyCount);
            var seqMs = Measure(world, bodyCount, ParallelStrategy.Sequential, 1);
            var parMs = Measure(world, bodyCount, ParallelStrategy.ParallelFor, threadCount);
            var speedup = seqMs / parMs;

            PrintRow(bodyCount, seqMs, parMs, speedup);
            rows.Add(new CsvRow(bodyCount, seqMs, parMs, speedup));
        }

        WriteCsv(Path.Combine(ResultsDir, "series1_convex.csv"), "bodyCount", rows);
        Console.WriteLine($"Saved to {ResultsDir}/series1_convex.csv");
        Console.WriteLine();
    }

    public static void RunSeries2()
    {
        PrintBenchmarkHeader();
        Console.WriteLine("Series 2: Thread count impact");
        const int bodyCount = 400;
        var world = MakeConvexWorld(bodyCount);

        var seqMs = Measure(world, bodyCount, ParallelStrategy.Sequential, 1);
        Console.WriteLine($"Sequential baseline ({bodyCount} bodies): {seqMs:F2} ms");

        int[] threadCounts = [2, 4, 8, 16];
        var rows = new List<CsvRow>();

        PrintHeader("Threads");

        foreach (var tc in threadCounts)
        {
            var parMs = Measure(world, bodyCount, ParallelStrategy.ParallelFor, tc);
            var speedup = seqMs / parMs;

            PrintRow(tc, seqMs, parMs, speedup);
            rows.Add(new CsvRow(tc, seqMs, parMs, speedup));
        }

        WriteCsv(Path.Combine(ResultsDir, "series2_convex.csv"), "threadCount", rows);
        Console.WriteLine($"Saved to {ResultsDir}/series2_convex.csv");
        Console.WriteLine();
    }

    public static void RunSeries3()
    {
        PrintBenchmarkHeader();
        Console.WriteLine("Series 3: Strategy comparison");
        const int bodyCount = 400;
        var threadCount = Environment.ProcessorCount;
        var world = MakeConvexWorld(bodyCount);

        var seqMs = Measure(world, bodyCount, ParallelStrategy.Sequential, 1);
        var parForMs = Measure(world, bodyCount, ParallelStrategy.ParallelFor, threadCount);
        var taskMs = Measure(world, bodyCount, ParallelStrategy.TaskBased, threadCount);
        var poolMs = Measure(world, bodyCount, ParallelStrategy.ThreadPool, threadCount);

        Console.WriteLine($"{"Strategy",-16} {"Time (ms)",14} {"Speedup",10}");
        Console.WriteLine(new string('-', 42));
        Console.WriteLine($"{"Sequential",-16} {seqMs,14:F2} {"1.00x",10}");
        Console.WriteLine($"{"Parallel.For",-16} {parForMs,14:F2} {seqMs / parForMs,9:F2}x");
        Console.WriteLine($"{"Task.Run",-16} {taskMs,14:F2} {seqMs / taskMs,9:F2}x");
        Console.WriteLine($"{"ThreadPool",-16} {poolMs,14:F2} {seqMs / poolMs,9:F2}x");

        var path = Path.Combine(ResultsDir, "series3_convex.csv");
        using var writer = new StreamWriter(path);
        writer.WriteLine("strategy,sequentialMs,strategyMs,speedup");
        writer.WriteLine($"Sequential,{seqMs:F4},{seqMs:F4},{1.0:F4}");
        writer.WriteLine($"ParallelFor,{seqMs:F4},{parForMs:F4},{seqMs / parForMs:F4}");
        writer.WriteLine($"TaskBased,{seqMs:F4},{taskMs:F4},{seqMs / taskMs:F4}");
        writer.WriteLine($"ThreadPool,{seqMs:F4},{poolMs:F4},{seqMs / poolMs:F4}");

        Console.WriteLine($"Saved to {ResultsDir}/series3_convex.csv");
        Console.WriteLine();
    }

    private static PhysicsWorld MakeConvexWorld(int bodyCount)
    {
        var rng = new Random(Seed);
        var boxHalf = MathF.Cbrt(bodyCount * 12f);

        var world = new PhysicsWorld
        {
            Gravity = new Vector3(0, -9.81f, 0),
            Restitution = 0.5f,
            Friction = 0.4f,
            SolverIterations = 1,
            SleepThreshold = 0f
        };

        AddWalls(world, boxHalf);

        for (int i = 0; i < bodyCount; i++)
        {
            var scale = 0.3f + (float)rng.NextDouble() * 0.5f;
            var verts = new Vector3[TetraVerts.Length];
            for (int v = 0; v < TetraVerts.Length; v++)
                verts[v] = TetraVerts[v] * scale;

            var margin = scale;
            var pos = new Vector3(
                RandomRange(rng, -boxHalf + margin, boxHalf - margin),
                RandomRange(rng, -boxHalf + margin, boxHalf - margin),
                RandomRange(rng, -boxHalf + margin, boxHalf - margin));
            var vel = new Vector3(
                RandomRange(rng, -2f, 2f),
                RandomRange(rng, -2f, 2f),
                RandomRange(rng, -2f, 2f));

            var body = new RigidBody(new ConvexMeshShape(verts), 1f + (float)rng.NextDouble() * 4f, pos)
            {
                Velocity = vel
            };
            world.AddBody(body);
        }

        return world;
    }

    private static void AddWalls(PhysicsWorld world, float halfSize)
    {
        var h = new Vector3(halfSize, halfSize, halfSize);
        var t = 0.5f;
        world.AddBody(new RigidBody(new BoxShape(h with { Y = t }), 0f, new Vector3(0, -halfSize - t, 0), isStatic: true));
        world.AddBody(new RigidBody(new BoxShape(h with { Y = t }), 0f, new Vector3(0, halfSize + t, 0), isStatic: true));
        world.AddBody(new RigidBody(new BoxShape(h with { X = t }), 0f, new Vector3(-halfSize - t, 0, 0), isStatic: true));
        world.AddBody(new RigidBody(new BoxShape(h with { X = t }), 0f, new Vector3(halfSize + t, 0, 0), isStatic: true));
        world.AddBody(new RigidBody(new BoxShape(h with { Z = t }), 0f, new Vector3(0, 0, -halfSize - t), isStatic: true));
        world.AddBody(new RigidBody(new BoxShape(h with { Z = t }), 0f, new Vector3(0, 0, halfSize + t), isStatic: true));
    }

    private static double Measure(PhysicsWorld template, int bodyCount, ParallelStrategy strategy, int threadCount)
    {
        var times = new double[TotalRuns];
        var sw = new Stopwatch();

        for (var run = 0; run < TotalRuns; run++)
        {
            var world = MakeConvexWorld(bodyCount);

            sw.Restart();
            for (var step = 0; step < StepsPerRun; step++)
                world.Simulate(0.02f, strategy, threadCount);
            sw.Stop();

            times[run] = sw.Elapsed.TotalMilliseconds;
        }

        double sum = 0;
        for (int i = WarmupRuns; i < TotalRuns; i++)
            sum += times[i];
        return sum / MeasuredRuns;
    }

    private static float RandomRange(Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }

    private static void PrintHeader(string paramName = "Bodies")
    {
        Console.WriteLine($"{paramName,-10} {"Seq (ms)",14} {"Par (ms)",14} {"Speedup",10}");
        Console.WriteLine(new string('-', 50));
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
