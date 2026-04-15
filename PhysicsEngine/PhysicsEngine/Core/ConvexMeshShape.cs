using System;
using System.Numerics;

namespace PhysicsEngine.Core;

public class ConvexMeshShape : IShape
{
    public Vector3[] Vertices { get; }
    public int[]? Triangles { get; }

    public ConvexMeshShape(Vector3[] vertices, int[]? triangles = null)
    {
        Vertices = vertices ?? throw new ArgumentNullException(nameof(vertices));
        Triangles = triangles;
    }

    public Matrix4x4 ComputeInertiaTensor(float mass)
    {
        var min = Vertices[0];
        var max = Vertices[0];
        foreach (var v in Vertices)
        {
            min = Vector3.Min(min, v);
            max = Vector3.Max(max, v);
        }

        var size = max - min;
        var dx = size.X; var dy = size.Y; var dz = size.Z;
        var ix = mass / 12f * (dy * dy + dz * dz);
        var iy = mass / 12f * (dx * dx + dz * dz);
        var iz = mass / 12f * (dx * dx + dy * dy);

        return new Matrix4x4(
            ix, 0, 0, 0,
            0, iy, 0, 0,
            0, 0, iz, 0,
            0, 0, 0, 1
        );
    }

    public Vector3[] GetWorldVertices(Vector3 position, Quaternion rotation)
    {
        var world = new Vector3[Vertices.Length];
        for (var i = 0; i < Vertices.Length; i++)
            world[i] = Vector3.Transform(Vertices[i], rotation) + position;
        return world;
    }

    public static Vector3[] GenerateSphereVertices(float radius, int subdivisions = 2)
    {
        var t = (1f + MathF.Sqrt(5f)) / 2f;
        var baseVerts = new Vector3[]
        {
            Vector3.Normalize(new Vector3(-1, t, 0)),
            Vector3.Normalize(new Vector3(1, t, 0)),
            Vector3.Normalize(new Vector3(-1, -t, 0)),
            Vector3.Normalize(new Vector3(1, -t, 0)),
            Vector3.Normalize(new Vector3(0, -1, t)),
            Vector3.Normalize(new Vector3(0, 1, t)),
            Vector3.Normalize(new Vector3(0, -1, -t)),
            Vector3.Normalize(new Vector3(0, 1, -t)),
            Vector3.Normalize(new Vector3(t, 0, -1)),
            Vector3.Normalize(new Vector3(t, 0, 1)),
            Vector3.Normalize(new Vector3(-t, 0, -1)),
            Vector3.Normalize(new Vector3(-t, 0, 1))
        };

        var tris = new (int a, int b, int c)[]
        {
            (0,11,5),(0,5,1),(0,1,7),(0,7,10),(0,10,11),
            (1,5,9),(5,11,4),(11,10,2),(10,7,6),(7,1,8),
            (3,9,4),(3,4,2),(3,2,6),(3,6,8),(3,8,9),
            (4,9,5),(2,4,11),(6,2,10),(8,6,7),(9,8,1)
        };

        var triList = new System.Collections.Generic.List<(int, int, int)>(tris);
        var vertList = new System.Collections.Generic.List<Vector3>(baseVerts);
        var midCache = new System.Collections.Generic.Dictionary<long, int>();

        for (var s = 0; s < subdivisions; s++)
        {
            var newTris = new System.Collections.Generic.List<(int, int, int)>();
            midCache.Clear();
            foreach (var (a, b, c) in triList)
            {
                var ab = GetMidpoint(vertList, midCache, a, b);
                var bc = GetMidpoint(vertList, midCache, b, c);
                var ca = GetMidpoint(vertList, midCache, c, a);
                newTris.Add((a, ab, ca));
                newTris.Add((b, bc, ab));
                newTris.Add((c, ca, bc));
                newTris.Add((ab, bc, ca));
            }
            triList = newTris;
        }

        var result = new Vector3[vertList.Count];
        for (var i = 0; i < vertList.Count; i++)
            result[i] = vertList[i] * radius;
        return result;
    }

    private static int GetMidpoint(System.Collections.Generic.List<Vector3> verts,
        System.Collections.Generic.Dictionary<long, int> cache, int a, int b)
    {
        var key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
        if (cache.TryGetValue(key, out var idx)) return idx;
        var mid = Vector3.Normalize((verts[a] + verts[b]) * 0.5f);
        idx = verts.Count;
        verts.Add(mid);
        cache[key] = idx;
        return idx;
    }

    public static Vector3[] GenerateBoxVertices(Vector3 halfExtents)
    {
        float x = halfExtents.X, y = halfExtents.Y, z = halfExtents.Z;
        return
        [
            new Vector3(-x, -y, -z), new Vector3(x, -y, -z),
            new Vector3(x, y, -z), new Vector3(-x, y, -z),
            new Vector3(-x, -y, z), new Vector3(x, -y, z),
            new Vector3(x, y, z), new Vector3(-x, y, z)
        ];
    }
}
