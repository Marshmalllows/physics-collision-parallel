using System;
using System.Collections.Generic;
using System.Numerics;
using PhysicsEngine.Collision;

namespace PhysicsEngine.Dynamics;

public static class CollisionResolver
{
    private const float PenetrationCorrectionPercent = 1.0f;
    private const float PenetrationSlop = 0.005f;

    public static void Resolve(CollisionPair pair, float restitution, float friction)
    {
        var a = pair.BodyA;
        var b = pair.BodyB;
        var contact = pair.Contact;
        var normal = contact.Normal;

        var rA = contact.Position - a.Position;
        var rB = contact.Position - b.Position;

        var invIA = WorldInverseInertia(a.InverseInertiaTensor, a.Rotation);
        var invIB = WorldInverseInertia(b.InverseInertiaTensor, b.Rotation);

        var velA = a.Velocity + Vector3.Cross(a.AngularVelocity, rA);
        var velB = b.Velocity + Vector3.Cross(b.AngularVelocity, rB);
        var relVel = velB - velA;

        var contactVel = Vector3.Dot(relVel, normal);

        if (contactVel < 0)
        {
            var raCrossN = Vector3.Cross(rA, normal);
            var rbCrossN = Vector3.Cross(rB, normal);

            var angTermA = Vector3.Cross(TransformByInertia(invIA, raCrossN), rA);
            var angTermB = Vector3.Cross(TransformByInertia(invIB, rbCrossN), rB);

            var denominator = a.InverseMass + b.InverseMass + Vector3.Dot(angTermA + angTermB, normal);

            if (denominator > 1e-12f)
            {
                var j = -(1f + restitution) * contactVel / denominator;
                var impulse = j * normal;

                if (!a.IsStatic)
                {
                    a.Velocity -= a.InverseMass * impulse;
                    a.AngularVelocity -= TransformByInertia(invIA, Vector3.Cross(rA, impulse));
                }

                if (!b.IsStatic)
                {
                    b.Velocity += b.InverseMass * impulse;
                    b.AngularVelocity += TransformByInertia(invIB, Vector3.Cross(rB, impulse));
                }

                if (friction > 0)
                {
                    velA = a.Velocity + Vector3.Cross(a.AngularVelocity, rA);
                    velB = b.Velocity + Vector3.Cross(b.AngularVelocity, rB);
                    relVel = velB - velA;

                    var tangentVel = relVel - Vector3.Dot(relVel, normal) * normal;
                    var tangentLenSq = tangentVel.LengthSquared();

                    if (tangentLenSq > 1e-12f)
                    {
                        var tangent = tangentVel / MathF.Sqrt(tangentLenSq);
                        var jt = -Vector3.Dot(relVel, tangent) / denominator;

                        var maxFriction = friction * j;
                        jt = Math.Clamp(jt, -maxFriction, maxFriction);

                        var frictionImpulse = jt * tangent;

                        if (!a.IsStatic)
                        {
                            a.Velocity -= a.InverseMass * frictionImpulse;
                            a.AngularVelocity -= TransformByInertia(invIA, Vector3.Cross(rA, frictionImpulse));
                        }

                        if (!b.IsStatic)
                        {
                            b.Velocity += b.InverseMass * frictionImpulse;
                            b.AngularVelocity += TransformByInertia(invIB, Vector3.Cross(rB, frictionImpulse));
                        }
                    }
                }
            }
        }

        var totalInvMass = a.InverseMass + b.InverseMass;
        if (contact.PenetrationDepth > PenetrationSlop && totalInvMass > 0)
        {
            var correctionMag = (contact.PenetrationDepth - PenetrationSlop)
                                / totalInvMass
                                * PenetrationCorrectionPercent;
            var correction = correctionMag * normal;

            if (!a.IsStatic)
                a.Position -= a.InverseMass * correction;
            if (!b.IsStatic)
                b.Position += b.InverseMass * correction;

            var aInto = Vector3.Dot(a.Velocity, normal);
            if (!a.IsStatic && aInto > 0)
                a.Velocity -= aInto * normal;

            var bInto = Vector3.Dot(b.Velocity, normal);
            if (!b.IsStatic && bInto < 0)
                b.Velocity -= bInto * normal;
        }
    }

    public static void ResolveAll(List<CollisionPair> pairs, float restitution, float friction)
    {
        foreach (var pair in pairs)
            Resolve(pair, restitution, friction);
    }

    private static Matrix4x4 WorldInverseInertia(Matrix4x4 localInvI, Quaternion rotation)
    {
        var r = Matrix4x4.CreateFromQuaternion(rotation);
        var rt = Matrix4x4.Transpose(r);
        return r * localInvI * rt;
    }

    private static Vector3 TransformByInertia(Matrix4x4 invI, Vector3 v)
    {
        return new Vector3(
            invI.M11 * v.X + invI.M12 * v.Y + invI.M13 * v.Z,
            invI.M21 * v.X + invI.M22 * v.Y + invI.M23 * v.Z,
            invI.M31 * v.X + invI.M32 * v.Y + invI.M33 * v.Z
        );
    }
}
