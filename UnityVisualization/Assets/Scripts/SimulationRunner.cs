using UnityEngine;
using PhysicsEngine.Core;
using PhysicsEngine.Dynamics;
using SysNumerics = System.Numerics;

public class SimulationRunner : MonoBehaviour
{
    [Header("Room")]
    public float roomSize = 20f;

    [Header("Bodies")]
    public int sphereCount = 10;
    public int boxCount = 5;

    [Header("Spawn")]
    public float minSphereRadius = 0.5f;
    public float maxSphereRadius = 1.5f;
    public float minBoxHalfExtent = 0.3f;
    public float maxBoxHalfExtent = 1.0f;
    public float minSpeed = 3f;
    public float maxSpeed = 8f;

    [Header("Physics")]
    public float gravity = -9.81f;
    public float restitution = 0.5f;
    public float friction = 0.4f;
    public int solverIterations = 1;
    [Range(0.9f, 1f)]
    public float linearDamping = 0.999f;
    [Range(0.9f, 1f)]
    public float angularDamping = 0.98f;

    public PhysicsWorld World { get; private set; }

    void Awake()
    {
        World = new PhysicsWorld
        {
            Gravity = new SysNumerics.Vector3(0f, gravity, 0f),
            Restitution = restitution,
            Friction = friction,
            SolverIterations = solverIterations,
            LinearDamping = linearDamping,
            AngularDamping = angularDamping
        };
        CreateWalls();
        SpawnBodies();
    }

    void FixedUpdate()
    {
        World.Simulate(Time.fixedDeltaTime);
    }

    void CreateWalls()
    {
        var half = roomSize / 2f;
        var wallThickness = 1f;
        var t = half + wallThickness / 2f;

        // floor / ceiling (Y axis)
        AddWall(new SysNumerics.Vector3(0, -t, 0), new SysNumerics.Vector3(half + wallThickness, wallThickness / 2f, half + wallThickness));
        AddWall(new SysNumerics.Vector3(0, t, 0), new SysNumerics.Vector3(half + wallThickness, wallThickness / 2f, half + wallThickness));

        // left / right (X axis)
        AddWall(new SysNumerics.Vector3(-t, 0, 0), new SysNumerics.Vector3(wallThickness / 2f, half + wallThickness, half + wallThickness));
        AddWall(new SysNumerics.Vector3(t, 0, 0), new SysNumerics.Vector3(wallThickness / 2f, half + wallThickness, half + wallThickness));

        // front / back (Z axis)
        AddWall(new SysNumerics.Vector3(0, 0, -t), new SysNumerics.Vector3(half + wallThickness, half + wallThickness, wallThickness / 2f));
        AddWall(new SysNumerics.Vector3(0, 0, t), new SysNumerics.Vector3(half + wallThickness, half + wallThickness, wallThickness / 2f));
    }

    void AddWall(SysNumerics.Vector3 position, SysNumerics.Vector3 halfExtents)
    {
        var wall = new RigidBody(new BoxShape(halfExtents), 0f, position, isStatic: true);
        World.AddBody(wall);
    }

    void SpawnBodies()
    {
        var half = roomSize / 2f;

        for (int i = 0; i < sphereCount; i++)
        {
            var radius = Random.Range(minSphereRadius, maxSphereRadius);
            var pos = RandomPositionInRoom(half - radius);
            var vel = RandomVelocity();
            var body = new RigidBody(new SphereShape(radius), 1f, pos) { Velocity = vel };
            World.AddBody(body);
        }

        for (int i = 0; i < boxCount; i++)
        {
            var he = new SysNumerics.Vector3(
                Random.Range(minBoxHalfExtent, maxBoxHalfExtent),
                Random.Range(minBoxHalfExtent, maxBoxHalfExtent),
                Random.Range(minBoxHalfExtent, maxBoxHalfExtent)
            );
            var maxHe = Mathf.Max(he.X, Mathf.Max(he.Y, he.Z));
            var pos = RandomPositionInRoom(half - maxHe);
            var vel = RandomVelocity();
            var body = new RigidBody(new BoxShape(he), 1f, pos) { Velocity = vel };
            World.AddBody(body);
        }
    }

    SysNumerics.Vector3 RandomPositionInRoom(float range)
    {
        return new SysNumerics.Vector3(
            Random.Range(-range, range),
            Random.Range(-range, range),
            Random.Range(-range, range)
        );
    }

    SysNumerics.Vector3 RandomVelocity()
    {
        var dir = new SysNumerics.Vector3(
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f),
            Random.Range(-1f, 1f)
        );
        dir = SysNumerics.Vector3.Normalize(dir);
        var speed = Random.Range(minSpeed, maxSpeed);
        return dir * speed;
    }
}
