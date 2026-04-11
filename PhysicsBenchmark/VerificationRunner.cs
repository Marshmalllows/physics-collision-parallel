using System.Numerics;
using PhysicsEngine.Dynamics;
using PhysicsEngine.Serialization;

namespace PhysicsBenchmark;

public static class VerificationRunner
{
    public static void Run()
    {
        Console.WriteLine("Verification: Sequential vs Parallel determinism");

        const int bodyCount = 1000;
        const int steps = 1000;
        const float eps = 0.01f;
        var threadCount = Environment.ProcessorCount;

        var boxHalf = MathF.Cbrt(bodyCount * 8f);
        var config = ScenarioBuilder.GenerateRandom(seed: 42, bodyCount,
            new Vector3(boxHalf, boxHalf, boxHalf));

        Console.WriteLine($"Bodies: {config.Bodies.Count}, Steps: {steps}, Threads: {threadCount}, Eps: {eps}");

        var worldSeq = ScenarioBuilder.BuildWorld(config);
        for (var i = 0; i < steps; i++)
            worldSeq.Simulate(config.TimeStep);

        var worldPar = ScenarioBuilder.BuildWorld(config);
        for (var i = 0; i < steps; i++)
            worldPar.Simulate(config.TimeStep, ParallelStrategy.ParallelFor, threadCount);

        var mismatches = 0;
        var maxDelta = 0f;
        var worstBody = -1;

        for (var i = 0; i < worldSeq.Bodies.Count; i++)
        {
            var posSeq = worldSeq.Bodies[i].Position;
            var posPar = worldPar.Bodies[i].Position;
            var delta = (posSeq - posPar).Length();

            if (delta > eps)
            {
                mismatches++;
                if (mismatches <= 5)
                    Console.WriteLine($"MISMATCH body {i}: seq={posSeq} par={posPar} delta={delta:F6}");
            }

            if (delta > maxDelta)
            {
                maxDelta = delta;
                worstBody = i;
            }
        }

        Console.WriteLine($"Max delta: {maxDelta:F6} (body {worstBody})");

        Console.WriteLine(mismatches == 0
            ? "PASS: all positions match within eps."
            : $"FAIL: {mismatches}/{worldSeq.Bodies.Count} bodies exceed eps.");

        Console.WriteLine();
    }
}
