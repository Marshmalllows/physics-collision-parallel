using System.Numerics;

namespace PhysicsEngine.Core;

public class BoxShape(Vector3 halfExtents) : IShape
{
    public Vector3 HalfExtents { get; } = halfExtents;

    public Matrix4x4 ComputeInertiaTensor(float mass)
    {
        var dx = 2f * HalfExtents.X;
        var dy = 2f * HalfExtents.Y;
        var dz = 2f * HalfExtents.Z;

        var ix = mass / 12f * (dy * dy + dz * dz);
        var iy = mass / 12f * (dx * dx + dz * dz);
        var iz = mass / 12f * (dx * dx + dy * dy);

        return new Matrix4x4(
            ix, 0, 0, 0,
            0, iy, 0, 0,
            0, 0, iz, 0,
            0, 0, 0, 1
        );
    }
}
