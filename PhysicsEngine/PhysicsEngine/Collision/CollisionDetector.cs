using System.Collections.Concurrent;
using System.Numerics;
using PhysicsEngine.Core;

namespace PhysicsEngine.Collision;

public static class CollisionDetector
{
    public static ContactPoint? SphereSphere(RigidBody a, RigidBody b)
    {
        var sa = (SphereShape)a.Shape;
        var sb = (SphereShape)b.Shape;

        var diff = b.Position - a.Position;
        var distSq = diff.LengthSquared();
        var radiusSum = sa.Radius + sb.Radius;

        if (distSq >= radiusSum * radiusSum)
            return null;

        var dist = MathF.Sqrt(distSq);
        var normal = dist > 1e-8f ? diff / dist : Vector3.UnitY;
        var penetration = radiusSum - dist;
        var contactPos = a.Position + normal * sa.Radius;

        return new ContactPoint(contactPos, normal, penetration);
    }

    public static ContactPoint? SphereBox(RigidBody sphere, RigidBody box)
    {
        var ss = (SphereShape)sphere.Shape;
        var bs = (BoxShape)box.Shape;

        var invRot = Quaternion.Conjugate(box.Rotation);
        var local = Vector3.Transform(sphere.Position - box.Position, invRot);

        var closest = Vector3.Clamp(local, -bs.HalfExtents, bs.HalfExtents);

        var diff = local - closest;
        var distSq = diff.LengthSquared();

        if (distSq >= ss.Radius * ss.Radius)
            return null;

        if (distSq > 1e-12f)
        {
            var dist = MathF.Sqrt(distSq);
            var localNormal = diff / dist;
            var penetration = ss.Radius - dist;
            var worldNormal = Vector3.Transform(localNormal, box.Rotation);
            var contactPos = sphere.Position - worldNormal * (ss.Radius - penetration * 0.5f);
            return new ContactPoint(contactPos, worldNormal, penetration);
        }

        var absLocal = Vector3.Abs(local);
        var faceDistances = bs.HalfExtents - absLocal;

        int minAxis;
        if (faceDistances.X <= faceDistances.Y && faceDistances.X <= faceDistances.Z)
            minAxis = 0;
        else if (faceDistances.Y <= faceDistances.Z)
            minAxis = 1;
        else
            minAxis = 2;

        var localNormal2 = Vector3.Zero;
        float sign;
        float faceDist;
        switch (minAxis)
        {
            case 0:
                sign = MathF.Sign(local.X);
                if (sign == 0) sign = 1;
                localNormal2 = new Vector3(sign, 0, 0);
                faceDist = faceDistances.X;
                break;
            case 1:
                sign = MathF.Sign(local.Y);
                if (sign == 0) sign = 1;
                localNormal2 = new Vector3(0, sign, 0);
                faceDist = faceDistances.Y;
                break;
            default:
                sign = MathF.Sign(local.Z);
                if (sign == 0) sign = 1;
                localNormal2 = new Vector3(0, 0, sign);
                faceDist = faceDistances.Z;
                break;
        }

        var penetration2 = ss.Radius + faceDist;
        var worldNormal2 = Vector3.Transform(localNormal2, box.Rotation);
        var contactPos2 = sphere.Position - worldNormal2 * (ss.Radius - penetration2 * 0.5f);
        return new ContactPoint(contactPos2, worldNormal2, penetration2);
    }

    public static ContactPoint? BoxBox(RigidBody a, RigidBody b)
    {
        var sa = (BoxShape)a.Shape;
        var sb = (BoxShape)b.Shape;

        var rotA = Matrix4x4.CreateFromQuaternion(a.Rotation);
        var rotB = Matrix4x4.CreateFromQuaternion(b.Rotation);

        Span<Vector3> axesA =
        [
            new(rotA.M11, rotA.M12, rotA.M13),
            new(rotA.M21, rotA.M22, rotA.M23),
            new(rotA.M31, rotA.M32, rotA.M33)
        ];
        Span<Vector3> axesB =
        [
            new(rotB.M11, rotB.M12, rotB.M13),
            new(rotB.M21, rotB.M22, rotB.M23),
            new(rotB.M31, rotB.M32, rotB.M33)
        ];

        float[] extA = [sa.HalfExtents.X, sa.HalfExtents.Y, sa.HalfExtents.Z];
        float[] extB = [sb.HalfExtents.X, sb.HalfExtents.Y, sb.HalfExtents.Z];

        var t = b.Position - a.Position;

        Span<float> r = stackalloc float[9];
        Span<float> absR = stackalloc float[9];
        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++)
        {
            r[i * 3 + j] = Vector3.Dot(axesA[i], axesB[j]);
            absR[i * 3 + j] = MathF.Abs(r[i * 3 + j]) + 1e-6f;
        }

        var minPenetration = float.MaxValue;
        var bestAxis = Vector3.Zero;
        
        for (var i = 0; i < 3; i++)
        {
            var ra = extA[i];
            var rb = extB[0] * absR[i * 3] + extB[1] * absR[i * 3 + 1] + extB[2] * absR[i * 3 + 2];
            var d = MathF.Abs(Vector3.Dot(t, axesA[i]));
            var pen = ra + rb - d;
            if (pen < 0) return null;
            if (pen < minPenetration)
            {
                minPenetration = pen;
                bestAxis = axesA[i];
                if (Vector3.Dot(t, bestAxis) < 0) bestAxis = -bestAxis;
            }
        }

        for (var j = 0; j < 3; j++)
        {
            var ra = extA[0] * absR[j] + extA[1] * absR[3 + j] + extA[2] * absR[6 + j];
            var rb = extB[j];
            var d = MathF.Abs(Vector3.Dot(t, axesB[j]));
            var pen = ra + rb - d;
            if (pen < 0) return null;
            if (pen < minPenetration)
            {
                minPenetration = pen;
                bestAxis = axesB[j];
                if (Vector3.Dot(t, bestAxis) < 0) bestAxis = -bestAxis;
            }
        }

        for (var i = 0; i < 3; i++)
        for (var j = 0; j < 3; j++)
        {
            var axis = Vector3.Cross(axesA[i], axesB[j]);
            var lenSq = axis.LengthSquared();
            if (lenSq < 1e-6f) continue;
            axis /= MathF.Sqrt(lenSq);

            var ra = 0f;
            var rb = 0f;
            for (var k = 0; k < 3; k++)
            {
                ra += extA[k] * MathF.Abs(Vector3.Dot(axesA[k], axis));
                rb += extB[k] * MathF.Abs(Vector3.Dot(axesB[k], axis));
            }

            var d = MathF.Abs(Vector3.Dot(t, axis));
            var pen = ra + rb - d;
            if (pen < 0) return null;
            if (pen < minPenetration)
            {
                minPenetration = pen;
                bestAxis = axis;
                if (Vector3.Dot(t, bestAxis) < 0) bestAxis = -bestAxis;
            }
        }

        var contactPos = a.Position + bestAxis * (minPenetration * 0.5f);
        return new ContactPoint(contactPos, bestAxis, minPenetration);
    }

    public static ContactPoint? Detect(RigidBody a, RigidBody b)
    {
        if (a.IsStatic && b.IsStatic) return null;
        return (a.Shape, b.Shape) switch
        {
            (SphereShape, SphereShape) => SphereSphere(a, b),
            (SphereShape, BoxShape) => SphereBox(a, b),
            (BoxShape, SphereShape) => Flip(SphereBox(b, a)),
            (BoxShape, BoxShape) => BoxBox(a, b),
            _ => null
        };
    }

    public static List<CollisionPair> DetectAll(List<RigidBody> bodies, bool parallel, int threadCount)
    {
        if (parallel)
        {
            var bag = new ConcurrentBag<CollisionPair>();
            var options = new ParallelOptions { MaxDegreeOfParallelism = threadCount };
            Parallel.For(0, bodies.Count, options, i =>
            {
                for (var j = i + 1; j < bodies.Count; j++)
                {
                    var contact = Detect(bodies[i], bodies[j]);
                    if (contact.HasValue)
                        bag.Add(new CollisionPair(bodies[i], bodies[j], contact.Value));
                }
            });
            return [.. bag];
        }

        var pairs = new List<CollisionPair>();
        for (var i = 0; i < bodies.Count; i++)
        for (var j = i + 1; j < bodies.Count; j++)
        {
            var contact = Detect(bodies[i], bodies[j]);
            if (contact.HasValue)
                pairs.Add(new CollisionPair(bodies[i], bodies[j], contact.Value));
        }
        return pairs;
    }

    private static ContactPoint? Flip(ContactPoint? cp)
    {
        if (cp is null) return null;
        var c = cp.Value;
        return new ContactPoint(c.Position, -c.Normal, c.PenetrationDepth);
    }
}
