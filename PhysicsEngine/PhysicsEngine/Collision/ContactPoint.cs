using System.Numerics;

namespace PhysicsEngine.Collision;

public readonly struct ContactPoint(Vector3 position, Vector3 normal, float penetrationDepth)
{
    public Vector3 Position { get; } = position;
    public Vector3 Normal { get; } = normal;
    public float PenetrationDepth { get; } = penetrationDepth;
}
