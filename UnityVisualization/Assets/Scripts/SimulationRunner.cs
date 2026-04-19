using UnityEngine;
using PhysicsEngine.Core;
using PhysicsEngine.Dynamics;
using PhysicsEngine.Serialization;
using SysNumerics = System.Numerics;

public class SimulationRunner : MonoBehaviour
{
    [Header("Scenario")]
    public string scenarioFile = "";

    [Header("Room")]
    public float roomSize = 20f;

    [Header("Bodies")]
    public int sphereCount = 10;
    public int boxCount = 5;
    public int convexMeshCount = 3;
    public string modelsDir = "Models";

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

    [Header("Parallelism")]
    public bool useParallel = true;
    public int threadCount = 0;

    public PhysicsWorld World { get; private set; }

    void Awake()
    {
        if (ScenarioSelector.PendingScenario != null)
        {
            scenarioFile = ScenarioSelector.PendingScenario;
            ScenarioSelector.PendingScenario = null;
        }

        if (ScenarioSelector.PendingParams.HasValue)
        {
            var p = ScenarioSelector.PendingParams.Value;
            roomSize = p.roomSize;
            sphereCount = p.sphereCount;
            boxCount = p.boxCount;
            convexMeshCount = p.convexMeshCount;
            gravity = p.gravity;
            restitution = p.restitution;
            friction = p.friction;
            solverIterations = p.solverIterations;
            linearDamping = p.linearDamping;
            angularDamping = p.angularDamping;
            minSpeed = p.minSpeed;
            maxSpeed = p.maxSpeed;
            useParallel = p.useParallel;
            ScenarioSelector.PendingParams = null;
        }

        if (!string.IsNullOrEmpty(scenarioFile))
        {
            LoadFromScenario(scenarioFile);
        }
        else
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
    }

    void LoadFromScenario(string fileName)
    {
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Scenarios", fileName);
        Debug.Log($"Loading scenario from: {path}");
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"Scenario file not found: {path}");
            return;
        }

        var config = ScenarioSerializer.LoadFromJson(path);
        roomSize = config.BoxHalfSize.X * 2f;
        World = ScenarioBuilder.BuildWorld(config, System.IO.Path.GetDirectoryName(path));
    }

    void FixedUpdate()
    {
        if (useParallel)
        {
            var tc = threadCount > 0 ? threadCount : System.Environment.ProcessorCount;
            World.Simulate(Time.fixedDeltaTime, ParallelStrategy.ParallelFor, tc);
        }
        else
        {
            World.Simulate(Time.fixedDeltaTime);
        }
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

        string[] objFiles = null;
        var searchDirs = new[]
        {
            System.IO.Path.Combine(Application.streamingAssetsPath, modelsDir),
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), modelsDir),
            System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), "..", modelsDir),
        };
        foreach (var dir in searchDirs)
        {
            if (System.IO.Directory.Exists(dir))
            {
                objFiles = System.IO.Directory.GetFiles(dir, "*.obj");
                if (objFiles.Length > 0) break;
            }
        }
        if (objFiles != null && objFiles.Length == 0)
            objFiles = null;

        for (var i = 0; i < sphereCount; i++)
        {
            var radius = Random.Range(minSphereRadius, maxSphereRadius);
            var pos = RandomPositionInRoom(half - radius);
            var vel = RandomVelocity();
            var body = new RigidBody(new SphereShape(radius), 1f, pos) { Velocity = vel };
            World.AddBody(body);
        }

        for (var i = 0; i < boxCount; i++)
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

        for (var i = 0; i < convexMeshCount; i++)
        {
            SysNumerics.Vector3[] verts;
            int[] tris = null;
            float extent;

            var scale = Random.Range(minBoxHalfExtent, maxBoxHalfExtent);

            if (objFiles != null && objFiles.Length > 0)
            {
                var file = objFiles[Random.Range(0, objFiles.Length)];
                var (objVerts, objTris) = ObjLoader.LoadWithFaces(file);
                verts = new SysNumerics.Vector3[objVerts.Length];
                for (int vi = 0; vi < objVerts.Length; vi++)
                    verts[vi] = objVerts[vi] * scale;
                tris = objTris.Length > 0 ? objTris : null;

                var min = verts[0]; var max = verts[0];
                foreach (var v in verts) { min = SysNumerics.Vector3.Min(min, v); max = SysNumerics.Vector3.Max(max, v); }
                extent = Mathf.Max((max - min).X, Mathf.Max((max - min).Y, (max - min).Z)) / 2f;
            }
            else
            {
                var he = new SysNumerics.Vector3(
                    Random.Range(minBoxHalfExtent, maxBoxHalfExtent),
                    Random.Range(minBoxHalfExtent, maxBoxHalfExtent),
                    Random.Range(minBoxHalfExtent, maxBoxHalfExtent)
                );
                verts = ConvexMeshShape.GenerateBoxVertices(he);
                extent = Mathf.Max(he.X, Mathf.Max(he.Y, he.Z));
            }

            var pos = RandomPositionInRoom(half - extent);
            var vel = RandomVelocity();
            var body = new RigidBody(new ConvexMeshShape(verts, tris), 1f, pos) { Velocity = vel };
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
