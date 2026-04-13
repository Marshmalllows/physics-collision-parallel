using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace PhysicsEngine.Collision;

public static class GjkEpa
{
    private const int GjkMax = 64;
    private const int EpaMax = 64;
    private const float Epsilon = 1e-4f;

    public static bool CheckIntersection(Vector3[] polyhedron1, Vector3[] polyhedron2,
        out Vector3 contactPoint, out float penetrationDepth, out Vector3 contactNormal)
    {
        contactPoint = Vector3.Zero;
        penetrationDepth = 0;
        contactNormal = Vector3.Zero;

        var simplex = new Simplex();
        var direction = polyhedron1[0] - polyhedron2[0];
        if (direction.LengthSquared() < 1e-10f)
            direction = Vector3.UnitX;

        var support = CalculateSupportPoint(polyhedron1, polyhedron2, direction);
        simplex.AddSupportPoint(support);
        direction = -support.Point;

        for (var gjkCnt = 0; gjkCnt < GjkMax; gjkCnt++)
        {
            support = CalculateSupportPoint(polyhedron1, polyhedron2, direction);

            if (Vector3.Dot(support.Point, direction) <= 0)
                return false;

            simplex.AddSupportPoint(support);

            if (simplex.ContainsOrigin(ref direction))
            {
                Epa(polyhedron1, polyhedron2, simplex, out contactNormal, out penetrationDepth, out contactPoint);
                return true;
            }
        }

        return false;
    }

    private struct SupportPoint
    {
        public Vector3 Point;
        public int Index1;
        public int Index2;

        public SupportPoint(Vector3 point, int index1, int index2)
        {
            Point = point;
            Index1 = index1;
            Index2 = index2;
        }
    }

    private static SupportPoint CalculateSupportPoint(Vector3[] polyhedron1, Vector3[] polyhedron2, Vector3 direction)
    {
        var support1 = GetSupportPoint(polyhedron1, direction, out int index1);
        var support2 = GetSupportPoint(polyhedron2, -direction, out int index2);
        return new SupportPoint(support1 - support2, index1, index2);
    }

    private static Vector3 GetSupportPoint(Vector3[] polyhedron, Vector3 direction, out int index)
    {
        var maxDot = double.MinValue;
        var support = Vector3.Zero;
        index = -1;

        for (var i = 0; i < polyhedron.Length; i++)
        {
            double dot = Vector3.Dot(polyhedron[i], direction);
            if (dot > maxDot)
            {
                maxDot = dot;
                support = polyhedron[i];
                index = i;
            }
        }

        return support;
    }

    private class Simplex
    {
        public List<SupportPoint> Vertices { get; } = [];

        public void AddSupportPoint(SupportPoint sp) => Vertices.Insert(0, sp);

        public bool ContainsOrigin(ref Vector3 direction)
        {
            return Vertices.Count switch
            {
                4 => ContainsOriginTetrahedron(ref direction),
                3 => ContainsOriginTriangle(ref direction),
                2 => ContainsOriginLine(ref direction),
                _ => false
            };
        }

        private bool ContainsOriginTetrahedron(ref Vector3 direction)
        {
            var a = Vertices[0]; var b = Vertices[1]; var c = Vertices[2]; var d = Vertices[3];
            var ab = b.Point - a.Point; var ac = c.Point - a.Point; var ad = d.Point - a.Point;
            var ao = -a.Point;

            if (Vector3.Dot(Vector3.Cross(ab, ac), ao) > 0)
            {
                Vertices.Clear(); Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
                return ContainsOriginTriangle(ref direction);
            }
            if (Vector3.Dot(Vector3.Cross(ac, ad), ao) > 0)
            {
                Vertices.Clear(); Vertices.Add(a); Vertices.Add(c); Vertices.Add(d);
                return ContainsOriginTriangle(ref direction);
            }
            if (Vector3.Dot(Vector3.Cross(ad, ab), ao) > 0)
            {
                Vertices.Clear(); Vertices.Add(a); Vertices.Add(d); Vertices.Add(b);
                return ContainsOriginTriangle(ref direction);
            }
            return true;
        }

        private bool ContainsOriginTriangle(ref Vector3 direction)
        {
            var a = Vertices[0]; var b = Vertices[1]; var c = Vertices[2];
            var ab = b.Point - a.Point; var ac = c.Point - a.Point; var ao = -a.Point;
            var abc = Vector3.Cross(ab, ac);

            if (abc.LengthSquared() < 1e-10f)
            {
                Vertices.Clear(); Vertices.Add(a); Vertices.Add(b);
                return ContainsOriginLine(ref direction);
            }

            if (Vector3.Dot(Vector3.Cross(abc, ac), ao) > 0)
            {
                if (Vector3.Dot(ac, ao) > 0)
                {
                    Vertices.Clear(); Vertices.Add(a); Vertices.Add(c);
                    direction = Vector3.Cross(Vector3.Cross(ac, ao), ac);
                    if (direction.LengthSquared() < 1e-10f)
                        direction = abc;
                }
                else
                {
                    Vertices.Clear(); Vertices.Add(a); Vertices.Add(b);
                    return ContainsOriginLine(ref direction);
                }
            }
            else
            {
                if (Vector3.Dot(Vector3.Cross(ab, abc), ao) > 0)
                {
                    Vertices.Clear(); Vertices.Add(a); Vertices.Add(b);
                    return ContainsOriginLine(ref direction);
                }

                if (Vector3.Dot(abc, ao) > 0)
                {
                    direction = abc;
                }
                else
                {
                    Vertices.Clear(); Vertices.Add(a); Vertices.Add(c); Vertices.Add(b);
                    direction = -abc;
                }
            }
            return false;
        }

        private bool ContainsOriginLine(ref Vector3 direction)
        {
            var a = Vertices[0]; var b = Vertices[1];
            var ab = b.Point - a.Point; var ao = -a.Point;

            if (Vector3.Dot(ab, ao) > 0)
            {
                direction = Vector3.Cross(Vector3.Cross(ab, ao), ab);
                if (direction.LengthSquared() < 1e-10f)
                {
                    direction = MathF.Abs(ab.X) < 0.9f
                        ? Vector3.Cross(ab, Vector3.UnitX)
                        : Vector3.Cross(ab, Vector3.UnitY);
                }
            }
            else
            {
                Vertices.Clear(); Vertices.Add(a);
                direction = ao;
            }
            return false;
        }
    }

    private static (List<Vector4> normals, int minFace) GetFaceNormals(List<SupportPoint> polytope, List<int> faces)
    {
        var normals = new List<Vector4>();
        var minTriangle = 0;
        var minDistance = float.MaxValue;

        for (var i = 0; i < faces.Count; i += 3)
        {
            var a = polytope[faces[i]].Point;
            var b = polytope[faces[i + 1]].Point;
            var c = polytope[faces[i + 2]].Point;

            var normal = Vector3.Cross(b - a, c - a);
            var l = normal.Length();
            float distance;

            if (l < 1e-8f)
            {
                normal = Vector3.Zero;
                distance = float.MaxValue;
            }
            else
            {
                normal /= l;
                distance = Vector3.Dot(normal, a);
            }

            if (distance < 0)
            {
                normal *= -1;
                distance *= -1;
            }

            normals.Add(new Vector4(normal, distance));

            if (distance < minDistance)
            {
                minTriangle = i / 3;
                minDistance = distance;
            }
        }

        return (normals, minTriangle);
    }

    private static void AddIfUniqueEdge(ref List<(int, int)> edges, List<int> faces, int a, int b)
    {
        (int, int) reverse = default;
        var found = false;
        foreach (var f in edges)
        {
            if (f.Item1 == faces[b] && f.Item2 == faces[a])
            {
                reverse = f;
                found = true;
                break;
            }
        }

        if (found) edges.Remove(reverse);
        else edges.Add((faces[a], faces[b]));
    }

    private static Vector3 V4To3(Vector4 v) => new Vector3(v.X, v.Y, v.Z);

    private static void Epa(Vector3[] polyhedron1, Vector3[] polyhedron2, Simplex simplex,
        out Vector3 contactNormal, out float penetrationDepth, out Vector3 contactPoint)
    {
        var polytope = simplex.Vertices;
        var faces = new List<int> { 0, 1, 2, 0, 3, 1, 0, 2, 3, 1, 3, 2 };
        var (normals, minFace) = GetFaceNormals(polytope, faces);

        var minNormal = V4To3(normals[minFace]);
        var minDistance = float.MaxValue;

        for (var epaCnt = 0; epaCnt < EpaMax && minDistance == float.MaxValue; epaCnt++)
        {
            minNormal = V4To3(normals[minFace]);
            minDistance = normals[minFace].W;

            var support = CalculateSupportPoint(polyhedron1, polyhedron2, minNormal);
            var sDistance = Vector3.Dot(minNormal, support.Point);

            if (MathF.Abs(sDistance - minDistance) > Epsilon)
            {
                minDistance = float.MaxValue;
                var uniqueEdges = new List<(int, int)>();

                for (var i = 0; i < normals.Count; i++)
                {
                    if (Vector3.Dot(V4To3(normals[i]), support.Point) - normals[i].W > 0)
                    {
                        var f = i * 3;
                        AddIfUniqueEdge(ref uniqueEdges, faces, f, f + 1);
                        AddIfUniqueEdge(ref uniqueEdges, faces, f + 1, f + 2);
                        AddIfUniqueEdge(ref uniqueEdges, faces, f + 2, f);
                        faces[f + 2] = faces[^1]; faces.RemoveAt(faces.Count - 1);
                        faces[f + 1] = faces[^1]; faces.RemoveAt(faces.Count - 1);
                        faces[f] = faces[^1]; faces.RemoveAt(faces.Count - 1);
                        normals[i] = normals[^1]; normals.RemoveAt(normals.Count - 1);
                        i--;
                    }
                }

                var newFaces = new List<int>();
                for (var i = 0; i < uniqueEdges.Count; i++)
                {
                    newFaces.Add(uniqueEdges[i].Item1);
                    newFaces.Add(uniqueEdges[i].Item2);
                    newFaces.Add(polytope.Count);
                }

                polytope.Add(support);

                var (newNormals, newMinFace) = GetFaceNormals(polytope, newFaces);

                var oldMinDistance = float.MaxValue;
                for (var i = 0; i < normals.Count; i++)
                {
                    if (normals[i].W < oldMinDistance)
                    {
                        oldMinDistance = normals[i].W;
                        minFace = i;
                    }
                }

                if (newNormals[newMinFace].W < oldMinDistance)
                    minFace = newMinFace + normals.Count;

                faces.AddRange(newFaces);
                normals.AddRange(newNormals);
            }
        }

        var pa = polytope[faces[minFace * 3]];
        var pb = polytope[faces[minFace * 3 + 1]];
        var pc = polytope[faces[minFace * 3 + 2]];

        var dist = Vector3.Dot(pa.Point, minNormal);
        var projectedPoint = -dist * minNormal;

        var (u, v, w) = GetBarycentricCoordinates(projectedPoint, pa.Point, pb.Point, pc.Point);

        var contactPoint1 = u * polyhedron1[pa.Index1] + v * polyhedron1[pb.Index1] + w * polyhedron1[pc.Index1];
        var contactPoint2 = u * polyhedron2[pa.Index2] + v * polyhedron2[pb.Index2] + w * polyhedron2[pc.Index2];

        contactPoint = (contactPoint1 + contactPoint2) / 2;
        contactNormal = minNormal;
        penetrationDepth = minDistance;
    }

    private static (float u, float v, float w) GetBarycentricCoordinates(Vector3 p, Vector3 a, Vector3 b, Vector3 c)
    {
        var v0 = b - a; var v1 = c - a; var v2 = p - a;
        var d00 = Vector3.Dot(v0, v0);
        var d01 = Vector3.Dot(v0, v1);
        var d11 = Vector3.Dot(v1, v1);
        var d20 = Vector3.Dot(v2, v0);
        var d21 = Vector3.Dot(v2, v1);
        var denom = d00 * d11 - d01 * d01;

        if (MathF.Abs(denom) <= Epsilon)
        {
            if (d00 <= Epsilon && d11 <= Epsilon)
                return (1, 0, 0);
            if (d00 > Epsilon)
            {
                var t = Vector3.Dot(v2, v0) / d00;
                return (1f - t, t, 0);
            }
            if (d11 > Epsilon)
            {
                var t = Vector3.Dot(v2, v1) / d11;
                return (1f - t, 0, t);
            }
            return (1, 0, 0);
        }

        var bv = (d11 * d20 - d01 * d21) / denom;
        var bw = (d00 * d21 - d01 * d20) / denom;
        return (1f - bv - bw, bv, bw);
    }
}
