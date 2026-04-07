using System;
using System.Numerics;
using PhysicsEngine.Core;
using PhysicsEngine.Dynamics;

namespace PhysicsEngine.Serialization;

public static class ScenarioBuilder
{
    public static PhysicsWorld BuildWorld(ScenarioConfig config)
    {
        var world = new PhysicsWorld
        {
            Gravity = config.Gravity,
            Restitution = config.Restitution,
            Friction = config.Friction,
            SolverIterations = config.SolverIterations
        };

        foreach (var bc in config.Bodies)
        {
            IShape shape = bc.ShapeType switch
            {
                ShapeType.Sphere => new SphereShape(bc.Radius),
                ShapeType.Box => new BoxShape(bc.HalfExtents),
                _ => throw new ArgumentException($"Unknown shape type: {bc.ShapeType}")
            };

            var body = new RigidBody(shape, bc.Mass, bc.Position, bc.IsStatic)
            {
                Rotation = bc.Rotation,
                Velocity = bc.Velocity,
                AngularVelocity = bc.AngularVelocity
            };

            world.AddBody(body);
        }

        return world;
    }

    public static ScenarioConfig GenerateRandom(int seed, int bodyCount, Vector3 boxHalfSize)
    {
        var rng = new Random(seed);
        var config = new ScenarioConfig
        {
            Seed = seed,
            BoxHalfSize = boxHalfSize,
            Gravity = new Vector3(0f, -9.81f, 0f),
            Restitution = 0.5f,
            Friction = 0.4f,
            SolverIterations = 1
        };

        AddWalls(config, boxHalfSize);

        for (var i = 0; i < bodyCount; i++)
        {
            var isSphere = rng.NextDouble() < 0.5;
            var bc = new BodyConfig
            {
                IsStatic = false,
                Mass = 1f + (float)rng.NextDouble() * 4f
            };

            float margin;
            if (isSphere)
            {
                bc.ShapeType = ShapeType.Sphere;
                bc.Radius = 0.3f + (float)rng.NextDouble() * 0.7f;
                margin = bc.Radius;
            }
            else
            {
                bc.ShapeType = ShapeType.Box;
                bc.HalfExtents = new Vector3(
                    0.3f + (float)rng.NextDouble() * 0.5f,
                    0.3f + (float)rng.NextDouble() * 0.5f,
                    0.3f + (float)rng.NextDouble() * 0.5f
                );
                margin = Math.Max(bc.HalfExtentX, Math.Max(bc.HalfExtentY, bc.HalfExtentZ));
            }

            bc.Position = new Vector3(
                RandomRange(rng, -boxHalfSize.X + margin, boxHalfSize.X - margin),
                RandomRange(rng, -boxHalfSize.Y + margin, boxHalfSize.Y - margin),
                RandomRange(rng, -boxHalfSize.Z + margin, boxHalfSize.Z - margin)
            );

            bc.Velocity = new Vector3(
                RandomRange(rng, -2f, 2f),
                RandomRange(rng, -2f, 2f),
                RandomRange(rng, -2f, 2f)
            );

            config.Bodies.Add(bc);
        }

        return config;
    }

    private static void AddWalls(ScenarioConfig config, Vector3 halfSize)
    {
        const float wallThickness = 1f;
        const float wallHalf = wallThickness / 2f;

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { Y = wallHalf },
            Position = new Vector3(0f, -halfSize.Y - wallHalf, 0f),
            Mass = 0f,
            IsStatic = true
        });

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { Y = wallHalf },
            Position = new Vector3(0f, halfSize.Y + wallHalf, 0f),
            Mass = 0f,
            IsStatic = true
        });

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { X = wallHalf },
            Position = new Vector3(-halfSize.X - wallHalf, 0f, 0f),
            Mass = 0f,
            IsStatic = true
        });

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { X = wallHalf },
            Position = new Vector3(halfSize.X + wallHalf, 0f, 0f),
            Mass = 0f,
            IsStatic = true
        });

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { Z = wallHalf },
            Position = new Vector3(0f, 0f, -halfSize.Z - wallHalf),
            Mass = 0f,
            IsStatic = true
        });

        config.Bodies.Add(new BodyConfig
        {
            ShapeType = ShapeType.Box,
            HalfExtents = halfSize with { Z = wallHalf },
            Position = new Vector3(0f, 0f, halfSize.Z + wallHalf),
            Mass = 0f,
            IsStatic = true
        });
    }

    private static float RandomRange(Random rng, float min, float max)
    {
        return min + (float)rng.NextDouble() * (max - min);
    }
}
