using System.Numerics;

namespace PhysicsEngine.Core;

public class RigidBody
{
    public Vector3 Position { get; set; }
    public Quaternion Rotation { get; set; } = Quaternion.Identity;

    public Vector3 Velocity { get; set; }
    public Vector3 AngularVelocity { get; set; }

    public float Mass { get; }
    public float InverseMass { get; }

    public Matrix4x4 InertiaTensor { get; }
    public Matrix4x4 InverseInertiaTensor { get; }

    public IShape Shape { get; }
    public bool IsStatic { get; }

    public RigidBody(IShape shape, float mass, Vector3 position, bool isStatic = false)
    {
        Shape = shape;
        Position = position;
        IsStatic = isStatic;

        if (isStatic)
        {
            Mass = 0f;
            InverseMass = 0f;
            InertiaTensor = new Matrix4x4();
            InverseInertiaTensor = new Matrix4x4();
        }
        else
        {
            Mass = mass;
            InverseMass = 1f / mass;
            InertiaTensor = shape.ComputeInertiaTensor(mass);
            Matrix4x4.Invert(InertiaTensor, out var inv);
            InverseInertiaTensor = inv;
        }
    }
}
