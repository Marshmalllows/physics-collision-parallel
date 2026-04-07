using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace PhysicsEngine.Serialization;

public enum ShapeType
{
    Sphere,
    Box
}

public class BodyConfig
{
    public ShapeType ShapeType { get; set; }
    public float Radius { get; set; }
    public float HalfExtentX { get; set; }
    public float HalfExtentY { get; set; }
    public float HalfExtentZ { get; set; }
    public float Mass { get; set; }
    public float PositionX { get; set; }
    public float PositionY { get; set; }
    public float PositionZ { get; set; }
    public float RotationX { get; set; }
    public float RotationY { get; set; }
    public float RotationZ { get; set; }
    public float RotationW { get; set; } = 1f;
    public float VelocityX { get; set; }
    public float VelocityY { get; set; }
    public float VelocityZ { get; set; }
    public float AngularVelocityX { get; set; }
    public float AngularVelocityY { get; set; }
    public float AngularVelocityZ { get; set; }
    public bool IsStatic { get; set; }

    [JsonIgnore]
    public Vector3 HalfExtents
    {
        get => new(HalfExtentX, HalfExtentY, HalfExtentZ);
        set { HalfExtentX = value.X; HalfExtentY = value.Y; HalfExtentZ = value.Z; }
    }

    [JsonIgnore]
    public Vector3 Position
    {
        get => new(PositionX, PositionY, PositionZ);
        set { PositionX = value.X; PositionY = value.Y; PositionZ = value.Z; }
    }

    [JsonIgnore]
    public Quaternion Rotation
    {
        get => new(RotationX, RotationY, RotationZ, RotationW);
        set { RotationX = value.X; RotationY = value.Y; RotationZ = value.Z; RotationW = value.W; }
    }

    [JsonIgnore]
    public Vector3 Velocity
    {
        get => new(VelocityX, VelocityY, VelocityZ);
        set { VelocityX = value.X; VelocityY = value.Y; VelocityZ = value.Z; }
    }

    [JsonIgnore]
    public Vector3 AngularVelocity
    {
        get => new(AngularVelocityX, AngularVelocityY, AngularVelocityZ);
        set { AngularVelocityX = value.X; AngularVelocityY = value.Y; AngularVelocityZ = value.Z; }
    }
}

public class ScenarioConfig
{
    public int Seed { get; set; }
    public float TimeStep { get; set; } = 1f / 60f;
    public int TotalSteps { get; set; } = 600;
    public float BoxHalfSizeX { get; set; } = 10f;
    public float BoxHalfSizeY { get; set; } = 10f;
    public float BoxHalfSizeZ { get; set; } = 10f;
    public float GravityX { get; set; }
    public float GravityY { get; set; } = -9.81f;
    public float GravityZ { get; set; }
    public float Restitution { get; set; } = 0.5f;
    public float Friction { get; set; } = 0.4f;
    public int SolverIterations { get; set; } = 1;
    public List<BodyConfig> Bodies { get; set; } = [];

    [JsonIgnore]
    public Vector3 BoxHalfSize
    {
        get => new(BoxHalfSizeX, BoxHalfSizeY, BoxHalfSizeZ);
        set { BoxHalfSizeX = value.X; BoxHalfSizeY = value.Y; BoxHalfSizeZ = value.Z; }
    }

    [JsonIgnore]
    public Vector3 Gravity
    {
        get => new(GravityX, GravityY, GravityZ);
        set { GravityX = value.X; GravityY = value.Y; GravityZ = value.Z; }
    }
}
