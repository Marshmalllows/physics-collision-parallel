using System.Numerics;
using PhysicsEngine.Serialization;

namespace PhysicsBenchmark;

public static class ScenarioGenerator
{
    private static readonly string OutputDir = Path.GetFullPath(
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..",
            "UnityVisualization", "Assets", "StreamingAssets", "Scenarios"));

    public static void GenerateAll()
    {
        Directory.CreateDirectory(OutputDir);

        Console.WriteLine("Scenario Generation");
        Console.WriteLine("-------------------");

        Generate_WallBounce();
        Generate_HeadOnCollision();
        Generate_NewtonCradle();
        Generate_Stacking();
        Generate_MixedChaos();
        Generate_ConvexShowcase();

        Console.WriteLine();
        Console.WriteLine($"Output: {OutputDir}");
        Console.WriteLine();
    }

    private static void Save(ScenarioConfig config, string name)
    {
        var path = Path.Combine(OutputDir, name);
        ScenarioSerializer.SaveToJson(config, path);
        Console.WriteLine($"  Saved {name} ({config.Bodies.Count} bodies)");
    }

    private static void AddWalls(ScenarioConfig config, float halfSize)
    {
        var t = 0.5f;
        var h = halfSize;

        config.Bodies.Add(Wall(new Vector3(0, -h - t, 0), new Vector3(h + t, t, h + t)));
        config.Bodies.Add(Wall(new Vector3(0, h + t, 0), new Vector3(h + t, t, h + t)));
        config.Bodies.Add(Wall(new Vector3(-h - t, 0, 0), new Vector3(t, h + t, h + t)));
        config.Bodies.Add(Wall(new Vector3(h + t, 0, 0), new Vector3(t, h + t, h + t)));
        config.Bodies.Add(Wall(new Vector3(0, 0, -h - t), new Vector3(h + t, h + t, t)));
        config.Bodies.Add(Wall(new Vector3(0, 0, h + t), new Vector3(h + t, h + t, t)));
    }

    private static BodyConfig Wall(Vector3 pos, Vector3 halfExtents)
    {
        return new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfExtents,
            Position = pos,
            Mass = 0f,
            IsStatic = true
        };
    }

    private static BodyConfig Sphere(float radius, float mass, Vector3 pos, Vector3 vel = default)
    {
        return new BodyConfig
        {
            ShapeType = ShapeType.Sphere,
            Radius = radius,
            Mass = mass,
            Position = pos,
            Velocity = vel
        };
    }

    private static BodyConfig Box(Vector3 halfExtents, float mass, Vector3 pos, Vector3 vel = default)
    {
        return new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfExtents,
            Mass = mass,
            Position = pos,
            Velocity = vel
        };
    }

    private static BodyConfig ConvexMesh(string meshFile, float mass, Vector3 pos, Vector3 vel = default)
    {
        return new BodyConfig
        {
            ShapeType = ShapeType.ConvexMesh,
            MeshFile = meshFile,
            Mass = mass,
            Position = pos,
            Velocity = vel
        };
    }

    private static void Generate_WallBounce()
    {
        var config = new ScenarioConfig
        {
            Gravity = Vector3.Zero,
            Restitution = 1f,
            Friction = 0f,
            LinearDamping = 1f,
            AngularDamping = 1f,
            SleepThreshold = 0f,
            SolverIterations = 1,
            BoxHalfSize = new Vector3(10, 10, 10)
        };
        AddWalls(config, 10);
        config.Bodies.Add(Sphere(0.5f, 1f, new Vector3(0, 0, 0), new Vector3(5, 3, 2)));
        Save(config, "wall_bounce.json");
    }

    private static void Generate_HeadOnCollision()
    {
        var config = new ScenarioConfig
        {
            Gravity = Vector3.Zero,
            Restitution = 1f,
            Friction = 0f,
            LinearDamping = 1f,
            AngularDamping = 1f,
            SleepThreshold = 0f,
            SolverIterations = 1,
            BoxHalfSize = new Vector3(15, 15, 15)
        };
        AddWalls(config, 15);
        config.Bodies.Add(Sphere(1f, 1f, new Vector3(-5, 0, 0), new Vector3(4, 0, 0)));
        config.Bodies.Add(Sphere(1f, 1f, new Vector3(5, 0, 0), new Vector3(-4, 0, 0)));
        Save(config, "head_on_collision.json");
    }

    private static void Generate_NewtonCradle()
    {
        var config = new ScenarioConfig
        {
            Gravity = Vector3.Zero,
            Restitution = 1f,
            Friction = 0f,
            LinearDamping = 1f,
            AngularDamping = 1f,
            SleepThreshold = 0f,
            SolverIterations = 1,
            BoxHalfSize = new Vector3(15, 15, 15)
        };
        AddWalls(config, 15);

        var r = 0.5f;
        var spacing = r * 2.1f;

        for (int i = 0; i < 5; i++)
        {
            var x = (i - 2) * spacing;
            var vel = i == 0 ? new Vector3(4, 0, 0) : Vector3.Zero;
            config.Bodies.Add(Sphere(r, 1f, new Vector3(x, 0, 0), vel));
        }

        Save(config, "newton_cradle.json");
    }

    private static void Generate_Stacking()
    {
        var config = new ScenarioConfig
        {
            Gravity = new Vector3(0, -9.81f, 0),
            Restitution = 0.1f,
            Friction = 0.6f,
            SolverIterations = 4,
            BoxHalfSize = new Vector3(10, 20, 10)
        };
        AddWalls(config, 10);

        var he = new Vector3(0.5f, 0.5f, 0.5f);
        var floorTop = -10f;
        for (int i = 0; i < 10; i++)
        {
            var y = floorTop + he.Y + i * (he.Y * 2f);
            config.Bodies.Add(Box(he, 1f, new Vector3(0, y, 0)));
        }

        Save(config, "stacking.json");
    }

    private static void Generate_MixedChaos()
    {
        var config = new ScenarioConfig
        {
            Gravity = new Vector3(0, -9.81f, 0),
            Restitution = 0.5f,
            Friction = 0.4f,
            SolverIterations = 1,
            BoxHalfSize = new Vector3(10, 10, 10)
        };
        AddWalls(config, 10);

        var rng = new Random(42);

        for (int i = 0; i < 10; i++)
        {
            var pos = RandPos(rng, 8);
            var vel = RandVel(rng, 3);
            var r = 0.3f + (float)rng.NextDouble() * 0.5f;
            config.Bodies.Add(Sphere(r, 1f, pos, vel));
        }

        for (int i = 0; i < 10; i++)
        {
            var pos = RandPos(rng, 8);
            var vel = RandVel(rng, 3);
            var he = new Vector3(
                0.3f + (float)rng.NextDouble() * 0.4f,
                0.3f + (float)rng.NextDouble() * 0.4f,
                0.3f + (float)rng.NextDouble() * 0.4f);
            config.Bodies.Add(Box(he, 1f, pos, vel));
        }

        string[] meshFiles = ["../Models/pyramid.obj", "../Models/octahedron.obj", "../Models/cube.obj"];
        for (int i = 0; i < 10; i++)
        {
            var pos = RandPos(rng, 8);
            var vel = RandVel(rng, 3);
            var mesh = meshFiles[i % meshFiles.Length];
            config.Bodies.Add(ConvexMesh(mesh, 1f, pos, vel));
        }

        Save(config, "mixed_chaos.json");
    }

    private static void Generate_ConvexShowcase()
    {
        var config = new ScenarioConfig
        {
            Gravity = Vector3.Zero,
            Restitution = 0.8f,
            Friction = 0.2f,
            SleepThreshold = 0f,
            SolverIterations = 1,
            BoxHalfSize = new Vector3(10, 10, 10)
        };
        AddWalls(config, 10);

        var rng = new Random(77);
        string[] meshFiles = [
            "../Models/pyramid.obj",
            "../Models/octahedron.obj",
            "../Models/cube.obj",
            "../Models/diamond.obj",
            "../Models/pyramid.obj"
        ];

        for (int i = 0; i < 5; i++)
        {
            var pos = RandPos(rng, 6);
            var vel = RandVel(rng, 4);
            config.Bodies.Add(ConvexMesh(meshFiles[i], 1f, pos, vel));
        }

        Save(config, "convex_showcase.json");
    }

    private static Vector3 RandPos(Random rng, float range)
    {
        return new Vector3(
            (float)(rng.NextDouble() * range * 2 - range),
            (float)(rng.NextDouble() * range * 2 - range),
            (float)(rng.NextDouble() * range * 2 - range));
    }

    private static Vector3 RandVel(Random rng, float maxSpeed)
    {
        return new Vector3(
            (float)(rng.NextDouble() * maxSpeed * 2 - maxSpeed),
            (float)(rng.NextDouble() * maxSpeed * 2 - maxSpeed),
            (float)(rng.NextDouble() * maxSpeed * 2 - maxSpeed));
    }
}