using PhysicsEngine.Core;

namespace PhysicsEngine.Collision;

public readonly struct CollisionPair(RigidBody bodyA, RigidBody bodyB, ContactPoint contact)
{
    public RigidBody BodyA { get; } = bodyA;
    public RigidBody BodyB { get; } = bodyB;
    public ContactPoint Contact { get; } = contact;
}
