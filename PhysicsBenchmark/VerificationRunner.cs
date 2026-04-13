using System.Numerics;
using PhysicsEngine.Collision;
using PhysicsEngine.Core;
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

        RunConvexMeshTest();
    }

    private static void RunConvexMeshTest()
    {
        Console.WriteLine("Verification: ConvexMesh collision (GJK+EPA)");

        var cubeVerts = ConvexMeshShape.GenerateBoxVertices(new Vector3(0.5f, 0.5f, 0.5f));

        var bodyA = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, new Vector3(0, 0, 0));
        var bodyB = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, new Vector3(0.8f, 0, 0));
        var bodyC = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, new Vector3(5f, 0, 0));

        var hit = CollisionDetector.ConvexConvex(bodyA, bodyB);
        var miss = CollisionDetector.ConvexConvex(bodyA, bodyC);

        var passOverlap = hit != null && hit.Value.PenetrationDepth > 0;
        var passSeparated = miss == null;

        Console.WriteLine($"  Overlapping cubes (dist=0.8): {(passOverlap ? "HIT" : "MISS")} depth={hit?.PenetrationDepth:F4}");
        Console.WriteLine($"  Separated cubes (dist=5.0):   {(passSeparated ? "NO HIT" : "FALSE HIT")}");

        var sphere = new RigidBody(new SphereShape(0.5f), 1f, new Vector3(0, 0, 0));
        var meshNear = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, new Vector3(0.6f, 0, 0));
        var sphereHit = CollisionDetector.SphereConvex(sphere, meshNear);
        var passSphereConvex = sphereHit != null && sphereHit.Value.PenetrationDepth > 0;
        Console.WriteLine($"  Sphere vs ConvexMesh (dist=0.6): {(passSphereConvex ? "HIT" : "MISS")} depth={sphereHit?.PenetrationDepth:F4}");

        var box = new RigidBody(new BoxShape(new Vector3(0.5f, 0.5f, 0.5f)), 1f, new Vector3(0, 0, 0));
        var meshNear2 = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, new Vector3(0.7f, 0, 0));
        var boxHit = CollisionDetector.BoxConvex(box, meshNear2);
        var passBoxConvex = boxHit != null && boxHit.Value.PenetrationDepth > 0;
        Console.WriteLine($"  Box vs ConvexMesh (dist=0.7):    {(passBoxConvex ? "HIT" : "MISS")} depth={boxHit?.PenetrationDepth:F4}");

        var allPass = passOverlap && passSeparated && passSphereConvex && passBoxConvex;
        Console.WriteLine(allPass ? "PASS: all ConvexMesh collision tests passed." : "FAIL: some ConvexMesh tests failed.");

        Console.WriteLine();
    }
}
