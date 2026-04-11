using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using PhysicsEngine.Core;

namespace PhysicsEngine.Dynamics;

public static class Integrator
{
    public static void Integrate(RigidBody body, Vector3 gravity, float dt, float linearDamping, float angularDamping)
    {
        if (body.IsStatic)
            return;

        body.Velocity += gravity * dt;

        body.Velocity *= linearDamping;
        body.AngularVelocity *= angularDamping;

        const float maxLinearSpeed = 30f;
        const float maxAngularSpeed = 20f;

        var linSpeedSq = body.Velocity.LengthSquared();
        if (linSpeedSq > maxLinearSpeed * maxLinearSpeed)
            body.Velocity = Vector3.Normalize(body.Velocity) * maxLinearSpeed;

        var angSpeedSq = body.AngularVelocity.LengthSquared();
        if (angSpeedSq > maxAngularSpeed * maxAngularSpeed)
            body.AngularVelocity = Vector3.Normalize(body.AngularVelocity) * maxAngularSpeed;

        body.Position += body.Velocity * dt;

        var w = body.AngularVelocity;
        var spin = new Quaternion(w.X * dt * 0.5f, w.Y * dt * 0.5f, w.Z * dt * 0.5f, 0f);
        body.Rotation = Quaternion.Normalize(body.Rotation + spin * body.Rotation);
    }

    public static void IntegrateAll(List<RigidBody> bodies, Vector3 gravity, float dt,
        float linearDamping, float angularDamping, ParallelStrategy strategy, int threadCount)
    {
        switch (strategy)
        {
            case ParallelStrategy.ParallelFor:
            {
                var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
                Parallel.For(0, bodies.Count, options,
                    i => Integrate(bodies[i], gravity, dt, linearDamping, angularDamping));
                break;
            }
            case ParallelStrategy.TaskBased:
            {
                var chunkSize = Math.Max(1, (bodies.Count + threadCount - 1) / threadCount);
                var tasks = new Task[Math.Min(threadCount, (bodies.Count + chunkSize - 1) / chunkSize)];
                for (var t = 0; t < tasks.Length; t++)
                {
                    var start = t * chunkSize;
                    var end = Math.Min(start + chunkSize, bodies.Count);
                    tasks[t] = Task.Run(() =>
                    {
                        for (var i = start; i < end; i++)
                            Integrate(bodies[i], gravity, dt, linearDamping, angularDamping);
                    });
                }
                Task.WaitAll(tasks);
                break;
            }
            case ParallelStrategy.ThreadPool:
            {
                var chunkSize = Math.Max(1, (bodies.Count + threadCount - 1) / threadCount);
                var chunkCount = Math.Min(threadCount, (bodies.Count + chunkSize - 1) / chunkSize);
                using var done = new CountdownEvent(chunkCount);
                for (var t = 0; t < chunkCount; t++)
                {
                    var start = t * chunkSize;
                    var end = Math.Min(start + chunkSize, bodies.Count);
                    ThreadPool.QueueUserWorkItem(_ =>
                    {
                        for (var i = start; i < end; i++)
                            Integrate(bodies[i], gravity, dt, linearDamping, angularDamping);
                        done.Signal();
                    });
                }
                done.Wait();
                break;
            }
            default:
            {
                foreach (var body in bodies)
                    Integrate(body, gravity, dt, linearDamping, angularDamping);
                break;
            }
        }
    }
}
