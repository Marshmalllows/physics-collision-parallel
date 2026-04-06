using System.Numerics;
using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public static class Integrator
{
    public static void Integrate(RigidBody body, Vector3 gravity, float dt)
    {
        if (body.IsStatic)
            return;
        body.Velocity += gravity * dt;
        body.Position += body.Velocity * dt;
        var w = body.AngularVelocity;
        var spin = new Quaternion(w.X * dt * 0.5f, w.Y * dt * 0.5f, w.Z * dt * 0.5f, 0f);
        body.Rotation = Quaternion.Normalize(body.Rotation + spin * body.Rotation);
    }

    public static void IntegrateAll(List<RigidBody> bodies, Vector3 gravity, float dt, bool parallel, int threadCount)
    {
        if (parallel)
        {
            var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
            Parallel.For(0, bodies.Count, options, i => Integrate(bodies[i], gravity, dt));
        }
        else
        {
            foreach (var body in bodies)
                Integrate(body, gravity, dt);
        }
    }
}
