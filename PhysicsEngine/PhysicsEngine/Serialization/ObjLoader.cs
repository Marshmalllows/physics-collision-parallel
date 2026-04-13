using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Numerics;

namespace PhysicsEngine.Serialization;

public static class ObjLoader
{
    public static Vector3[] Load(string path)
    {
        var (vertices, _) = LoadWithFaces(path);
        return vertices;
    }

    public static (Vector3[] vertices, int[] triangles) LoadWithFaces(string path)
    {
        var vertices = new List<Vector3>();
        var triangles = new List<int>();

        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("v "))
            {
                var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;
                vertices.Add(new Vector3(
                    float.Parse(parts[1], CultureInfo.InvariantCulture),
                    float.Parse(parts[2], CultureInfo.InvariantCulture),
                    float.Parse(parts[3], CultureInfo.InvariantCulture)
                ));
            }
            else if (trimmed.StartsWith("f "))
            {
                var parts = trimmed.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 4) continue;

                var faceIndices = new List<int>();
                for (var i = 1; i < parts.Length; i++)
                {
                    var idx = parts[i].Split('/')[0];
                    faceIndices.Add(int.Parse(idx, CultureInfo.InvariantCulture) - 1);
                }

                for (var i = 1; i < faceIndices.Count - 1; i++)
                {
                    triangles.Add(faceIndices[0]);
                    triangles.Add(faceIndices[i]);
                    triangles.Add(faceIndices[i + 1]);
                }
            }
        }

        return (vertices.ToArray(), triangles.ToArray());
    }
}
