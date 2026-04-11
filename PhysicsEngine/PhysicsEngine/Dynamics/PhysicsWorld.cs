using System.Collections.Generic;
using System.Numerics;
using PhysicsEngine.Collision;
using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public class PhysicsWorld
{
    public List<RigidBody> Bodies { get; } = new();
    public Vector3 Gravity { get; set; } = new(0f, 0f, 0f);
    public float Restitution { get; set; } = 1f;
    public float Friction { get; set; } = 0.4f;
    public int SolverIterations { get; set; } = 1;
    public float SleepThreshold { get; set; } = 0.1f;
    public float AngularSleepThreshold { get; set; } = 0.05f;
    public float LinearDamping { get; set; } = 0.999f;
    public float AngularDamping { get; set; } = 0.98f;

    public void AddBody(RigidBody body)
    {
        Bodies.Add(body);
    }

    public void Simulate(float dt, ParallelStrategy strategy = ParallelStrategy.Sequential, int threadCount = 1)
    {
        Integrator.IntegrateAll(Bodies, Gravity, dt, LinearDamping, AngularDamping, strategy, threadCount);

        for (var i = 0; i < SolverIterations; i++)
        {
            var pairs = CollisionDetector.DetectAll(Bodies, strategy, threadCount);
            var iterRestitution = i == 0 ? Restitution : 0f;
            CollisionResolver.ResolveAll(pairs, iterRestitution, Friction);
        }

        if (SleepThreshold > 0)
        {
            var linSleepSq = SleepThreshold * SleepThreshold;
            var angSleepSq = AngularSleepThreshold * AngularSleepThreshold;
            foreach (var body in Bodies)
            {
                if (body.IsStatic) continue;
                if (body.Velocity.LengthSquared() < linSleepSq && body.AngularVelocity.LengthSquared() < angSleepSq)
                {
                    body.Velocity = Vector3.Zero;
                    body.AngularVelocity = Vector3.Zero;
                }
            }
        }
    }
}
