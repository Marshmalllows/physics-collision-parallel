using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public class PhysicsWorld
{
    public List<RigidBody> Bodies { get; } = new();

    public void AddBody(RigidBody body)
    {
        Bodies.Add(body);
    }

    public void Simulate(float dt, bool parallel = false, int threadCount = 1)
    {
        // TODO: broad phase
        // TODO: narrow phase collision detection
        // TODO: collision resolution (sequential)
        // TODO: integration (update positions/rotations)
        // TODO: verification (energy, momentum, penetration)
    }
}
