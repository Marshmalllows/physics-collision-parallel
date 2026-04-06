using System.Numerics;
using PhysicsEngine.Collision;
using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public class PhysicsWorld
{
    public List<RigidBody> Bodies { get; } = new();
    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);
    public float Restitution { get; set; } = 0.5f;

    public void AddBody(RigidBody body)
    {
        Bodies.Add(body);
    }

    public void Simulate(float dt, bool parallel = false, int threadCount = 1)
    {
        var pairs = CollisionDetector.DetectAll(Bodies, parallel, threadCount);
        CollisionResolver.ResolveAll(pairs, Restitution);
        Integrator.IntegrateAll(Bodies, Gravity, dt, parallel, threadCount);

        // TODO: verification (energy, momentum, penetration)
    }
}
