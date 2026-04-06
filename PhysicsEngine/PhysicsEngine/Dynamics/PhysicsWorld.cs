using System.Numerics;
using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public class PhysicsWorld
{
    public List<RigidBody> Bodies { get; } = new();
    public Vector3 Gravity { get; set; } = new(0f, -9.81f, 0f);

    public void AddBody(RigidBody body)
    {
        Bodies.Add(body);
    }

    public void Simulate(float dt, bool parallel = false, int threadCount = 1)
    {
        // TODO: broad phase
        // TODO: narrow phase collision detection
        // TODO: collision resolution (sequential)

        Integrator.IntegrateAll(Bodies, Gravity, dt, parallel, threadCount);

        // TODO: verification (energy, momentum, penetration)
    }
}
