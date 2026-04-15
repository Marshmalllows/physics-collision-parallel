using System.Numerics;
using PhysicsEngine.Collision;
using PhysicsEngine.Core;
using PhysicsEngine.Dynamics;

namespace PhysicsBenchmark;

public static class VerificationRunner
{
    public static void Run()
    {
        Console.WriteLine("Verification Tests (ConvexMesh)");
        Console.WriteLine("-------------------------------");
        Console.WriteLine();

        var results = new List<(string name, bool passed, string detail)>();

        results.Add(TestConvexEnergyConservation());
        results.Add(TestConvexMomentumConservation());
        results.Add(TestConvexNoPenetration());

        Console.WriteLine($"{"Test",-28} {"Result",-8} Detail");
        Console.WriteLine(new string('-', 70));
        var passCount = 0;
        foreach (var (name, passed, detail) in results)
        {
            passCount += passed ? 1 : 0;
            Console.WriteLine($"{name,-28} {(passed ? "PASS" : "FAIL"),-8} {detail}");
        }
        Console.WriteLine();
        Console.WriteLine($"{passCount}/{results.Count} tests passed.");
        Console.WriteLine();
    }

    private static float KineticEnergy(RigidBody b)
    {
        if (b.IsStatic) return 0;
        var linKE = 0.5f * b.Mass * b.Velocity.LengthSquared();
        var w = b.AngularVelocity;
        var Iw = new Vector3(
            b.InertiaTensor.M11 * w.X + b.InertiaTensor.M12 * w.Y + b.InertiaTensor.M13 * w.Z,
            b.InertiaTensor.M21 * w.X + b.InertiaTensor.M22 * w.Y + b.InertiaTensor.M23 * w.Z,
            b.InertiaTensor.M31 * w.X + b.InertiaTensor.M32 * w.Y + b.InertiaTensor.M33 * w.Z);
        var rotKE = 0.5f * Vector3.Dot(w, Iw);
        return linKE + rotKE;
    }

    private static Vector3 TotalMomentum(List<RigidBody> bodies)
    {
        var p = Vector3.Zero;
        foreach (var b in bodies)
            if (!b.IsStatic) p += b.Mass * b.Velocity;
        return p;
    }

    private static float TotalEnergy(List<RigidBody> bodies)
    {
        var e = 0f;
        foreach (var b in bodies) e += KineticEnergy(b);
        return e;
    }

    private static PhysicsWorld CreateConservativeWorld()
    {
        return new PhysicsWorld
        {
            Gravity = Vector3.Zero,
            Restitution = 1f,
            Friction = 0f,
            SolverIterations = 1,
            LinearDamping = 1f,
            AngularDamping = 1f,
            SleepThreshold = 0f
        };
    }

    private static (string, bool, string) TestConvexEnergyConservation()
    {
        const int count = 10;
        const int steps = 500;
        const float dt = 0.02f;

        var world = CreateConservativeWorld();
        var rng = new Random(321);
        var spread = 15f;
        var cubeVerts = ConvexMeshShape.GenerateBoxVertices(new Vector3(0.5f, 0.5f, 0.5f));

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3(
                (float)(rng.NextDouble() * spread - spread / 2),
                (float)(rng.NextDouble() * spread - spread / 2),
                (float)(rng.NextDouble() * spread - spread / 2));
            var vel = new Vector3(
                (float)(rng.NextDouble() * 4 - 2),
                (float)(rng.NextDouble() * 4 - 2),
                (float)(rng.NextDouble() * 4 - 2));
            var body = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, pos) { Velocity = vel };
            world.AddBody(body);
        }

        var startEnergy = TotalEnergy(world.Bodies);
        for (var i = 0; i < steps; i++) world.Simulate(dt);
        var endEnergy = TotalEnergy(world.Bodies);

        var ratio = startEnergy > 0.01f ? endEnergy / startEnergy : 1f;
        var pass = ratio <= 1.10f;
        return ("Energy conservation", pass, $"start={startEnergy:F2} end={endEnergy:F2} ratio={ratio:F4}");
    }

    private static (string, bool, string) TestConvexMomentumConservation()
    {
        const int count = 10;
        const int steps = 500;
        const float dt = 0.02f;

        var world = CreateConservativeWorld();
        var rng = new Random(654);
        var spread = 15f;
        var cubeVerts = ConvexMeshShape.GenerateBoxVertices(new Vector3(0.5f, 0.5f, 0.5f));

        for (int i = 0; i < count; i++)
        {
            var pos = new Vector3(
                (float)(rng.NextDouble() * spread - spread / 2),
                (float)(rng.NextDouble() * spread - spread / 2),
                (float)(rng.NextDouble() * spread - spread / 2));
            var vel = new Vector3(
                (float)(rng.NextDouble() * 4 - 2),
                (float)(rng.NextDouble() * 4 - 2),
                (float)(rng.NextDouble() * 4 - 2));
            var body = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, pos) { Velocity = vel };
            world.AddBody(body);
        }

        var startP = TotalMomentum(world.Bodies);
        for (var i = 0; i < steps; i++) world.Simulate(dt);
        var endP = TotalMomentum(world.Bodies);

        var diff = (endP - startP).Length();
        var startMag = startP.Length();
        var relDiff = startMag > 0.01f ? diff / startMag : diff;
        var pass = relDiff < 0.10f;
        return ("Momentum conservation", pass, $"delta={diff:F4} relative={relDiff:F4}");
    }

    private static (string, bool, string) TestConvexNoPenetration()
    {
        const int count = 20;
        const int steps = 500;
        const float dt = 0.02f;
        const float maxAllowed = 0.5f;

        var rng = new Random(789);
        var spread = 15f;
        var world = new PhysicsWorld
        {
            Gravity = new Vector3(0, -9.81f, 0),
            Restitution = 0.5f,
            Friction = 0.3f,
            SolverIterations = 1,
            LinearDamping = 0.999f,
            AngularDamping = 0.98f,
            SleepThreshold = 0f
        };

        world.AddBody(new RigidBody(new BoxShape(new Vector3(50, 1, 50)), 0f, new Vector3(0, -11, 0), isStatic: true));

        var cubeVerts = ConvexMeshShape.GenerateBoxVertices(new Vector3(0.5f, 0.5f, 0.5f));
        for (var i = 0; i < count; i++)
        {
            var pos = new Vector3(
                (float)(rng.NextDouble() * spread - spread / 2),
                (float)(rng.NextDouble() * spread),
                (float)(rng.NextDouble() * spread - spread / 2));
            var body = new RigidBody(new ConvexMeshShape(cubeVerts), 1f, pos);
            world.AddBody(body);
        }

        var maxPen = 0f;
        for (var step = 0; step < steps; step++)
        {
            world.Simulate(dt);
            var bodies = world.Bodies;
            for (var i = 0; i < bodies.Count; i++)
            for (var j = i + 1; j < bodies.Count; j++)
            {
                var cp = CollisionDetector.Detect(bodies[i], bodies[j]);
                if (cp != null && cp.Value.PenetrationDepth > maxPen)
                    maxPen = cp.Value.PenetrationDepth;
            }
        }

        var pass = maxPen < maxAllowed;
        return ("No penetration", pass, $"max={maxPen:F6} (limit {maxAllowed})");
    }
}
