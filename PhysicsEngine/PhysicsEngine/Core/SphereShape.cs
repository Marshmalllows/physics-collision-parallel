using System.Numerics;

namespace PhysicsEngine.Core;

public class SphereShape(float radius) : IShape
{
    public float Radius { get; } = radius;

    public Matrix4x4 ComputeInertiaTensor(float mass)
    {
        var i = 2f / 5f * mass * Radius * Radius;
        return new Matrix4x4(
            i, 0, 0, 0,
            0, i, 0, 0,
            0, 0, i, 0,
            0, 0, 0, 1
        );
    }
}
