using System.Numerics;

namespace PhysicsEngine.Core;

public interface IShape
{
    Matrix4x4 ComputeInertiaTensor(float mass);
}
