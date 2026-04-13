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
    public float AngularSleepThreshold { get; set; } = 0.3f;
    public int SleepFramesRequired { get; set; } = 30;
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

            foreach (var pair in pairs)
            {
                var a = pair.BodyA;
                var b = pair.BodyB;
                if (a.IsSleeping && !b.IsStatic && !b.IsSleeping)
                {
                    a.IsSleeping = false;
                    a.SleepFrames = 0;
                }
                if (b.IsSleeping && !a.IsStatic && !a.IsSleeping)
                {
                    b.IsSleeping = false;
                    b.SleepFrames = 0;
                }
            }
        }

        if (SleepThreshold > 0)
        {
            var linSleepSq = SleepThreshold * SleepThreshold;
            var angSleepSq = AngularSleepThreshold * AngularSleepThreshold;
            foreach (var body in Bodies)
            {
                if (body.IsStatic || body.IsSleeping) continue;

                // Zero small velocities independently to prevent EPA noise buildup
                var linSmall = body.Velocity.LengthSquared() < linSleepSq;
                var angSmall = body.AngularVelocity.LengthSquared() < angSleepSq;

                if (linSmall) body.Velocity = Vector3.Zero;
                if (angSmall) body.AngularVelocity = Vector3.Zero;

                // Energy-based sleep: use total kinetic energy instead of separate thresholds
                var energy = body.Velocity.LengthSquared() + body.AngularVelocity.LengthSquared();
                if (energy < linSleepSq + angSleepSq)
                {
                    body.Velocity = Vector3.Zero;
                    body.AngularVelocity = Vector3.Zero;
                    body.SleepFrames++;
                    if (body.SleepFrames >= SleepFramesRequired)
                        body.IsSleeping = true;
                }
                else
                {
                    body.SleepFrames = 0;
                }
            }
        }
    }

}
