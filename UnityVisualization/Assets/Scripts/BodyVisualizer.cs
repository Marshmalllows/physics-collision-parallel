using UnityEngine;
using PhysicsEngine.Core;
using SysNumerics = System.Numerics;

public class BodyVisualizer : MonoBehaviour
{
    [Tooltip("Assign any URP Lit material here (used as template for cloning)")]
    public Material urpLitTemplate;

    SimulationRunner runner;
    GameObject[] visuals;

    static readonly Color[] sphereColors =
    {
        new(0.2f, 0.45f, 0.95f),
        new(0.15f, 0.75f, 0.55f),
        new(0.85f, 0.25f, 0.35f),
        new(0.55f, 0.3f, 0.85f),
        new(0.95f, 0.75f, 0.15f),
    };

    static readonly Color[] boxColors =
    {
        new(0.95f, 0.5f, 0.15f),
        new(0.85f, 0.2f, 0.55f),
        new(0.3f, 0.8f, 0.3f),
    };

    static Shader _sharedURPShader;

    Material CreateURPMaterial(Color color, float metallic, float smoothness)
    {
        if (urpLitTemplate != null)
        {
            var mat = new Material(urpLitTemplate);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        if (_sharedURPShader == null)
        {
            _sharedURPShader = Shader.Find("Universal Render Pipeline/Lit");
            if (_sharedURPShader == null)
                _sharedURPShader = Shader.Find("Universal Render Pipeline/Simple Lit");
        }

        if (_sharedURPShader != null)
        {
            var mat = new Material(_sharedURPShader);
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            return mat;
        }

        Debug.LogWarning("URP Lit shader not found — materials will be pink");
        return new Material(Shader.Find("Standard")) { color = color };
    }

    void ApplyMat(MeshRenderer renderer, Color color, float metallic, float smoothness)
    {
        renderer.material = CreateURPMaterial(color, metallic, smoothness);
    }

    void Start()
    {
        runner = FindAnyObjectByType<SimulationRunner>();
        if (runner == null || runner.World == null)
            return;

        SetupLighting(runner.roomSize);

        var bodies = runner.World.Bodies;
        visuals = new GameObject[bodies.Count];

        var wallCount = 0;
        for (var i = 0; i < bodies.Count; i++)
        {
            if (bodies[i].IsStatic) wallCount++;
            else break;
        }

        if (wallCount == 6)
        {
            var roomObj = BuildRoom(runner.roomSize);
            for (var i = 0; i < 6; i++)
                visuals[i] = roomObj;
        }

        int sphereIdx = 0, boxIdx = 0;

        for (var i = wallCount; i < bodies.Count; i++)
        {
            var body = bodies[i];
            GameObject go;

            if (body.Shape is SphereShape sphere)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                var diameter = sphere.Radius * 2f;
                go.transform.localScale = new Vector3(diameter, diameter, diameter);
            }
            else if (body.Shape is BoxShape box)
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.transform.localScale = new Vector3(
                    box.HalfExtents.X * 2f,
                    box.HalfExtents.Y * 2f,
                    box.HalfExtents.Z * 2f
                );
            }
            else if (body.Shape is ConvexMeshShape mesh)
            {
                go = BuildConvexMeshVisual(mesh);
            }
            else
            {
                go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            }

            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null)
            {
                if (body.Shape is SphereShape)
                {
                    var c = sphereColors[sphereIdx++ % sphereColors.Length];
                    ApplyMat(renderer, c, 0.3f, 0.8f);
                }
                else
                {
                    var c = boxColors[boxIdx++ % boxColors.Length];
                    ApplyMat(renderer, c, 0.1f, 0.5f);
                }
            }

            go.name = $"Body_{i}";
            SyncTransform(go.transform, body);
            visuals[i] = go;
        }
    }

    void Update()
    {
        if (runner == null || runner.World == null || visuals == null)
            return;

        var bodies = runner.World.Bodies;
        for (var i = 0; i < bodies.Count; i++)
        {
            if (visuals[i] == null || bodies[i].IsStatic)
                continue;
            SyncTransform(visuals[i].transform, bodies[i]);
        }
    }

    static void SyncTransform(Transform t, RigidBody body)
    {
        t.position = ToUnity(body.Position);
        t.rotation = ToUnity(body.Rotation);
    }

    static Vector3 ToUnity(SysNumerics.Vector3 v)
    {
        return new Vector3(v.X, v.Y, v.Z);
    }

    static Quaternion ToUnity(SysNumerics.Quaternion q)
    {
        return new Quaternion(q.X, q.Y, q.Z, q.W);
    }

    static void SetupLighting(float roomSize)
    {
        var half = roomSize / 2f;
        var range = roomSize * 2.5f;

        // Remove default directional light
        var existing = FindAnyObjectByType<Light>();
        if (existing != null)
            Destroy(existing.gameObject);

        // Main spotlight from ceiling pointing down
        var mainLight = new GameObject("MainLight");
        mainLight.transform.position = new Vector3(0, half, 0);
        mainLight.transform.rotation = Quaternion.Euler(90, 0, 0);
        var ml = mainLight.AddComponent<Light>();
        ml.type = LightType.Spot;
        ml.color = new Color(1f, 0.98f, 0.93f);
        ml.intensity = 250f;
        ml.range = range;
        ml.spotAngle = 120f;
        ml.innerSpotAngle = 60f;
        ml.shadows = LightShadows.Soft;
        ml.shadowStrength = 0.7f;

        // Second point light lower for fill
        var mainLight2 = new GameObject("MainLight2");
        mainLight2.transform.position = new Vector3(0, -half * 0.2f, 0);
        var ml2 = mainLight2.AddComponent<Light>();
        ml2.type = LightType.Point;
        ml2.color = new Color(1f, 0.98f, 0.95f);
        ml2.intensity = 50f;
        ml2.range = range;
        ml2.shadows = LightShadows.Soft;
        ml2.shadowStrength = 0.4f;

        // Fill light from the side
        var fillLight = new GameObject("FillLight");
        fillLight.transform.position = new Vector3(-half * 0.7f, half * 0.3f, half * 0.5f);
        var fl = fillLight.AddComponent<Light>();
        fl.type = LightType.Point;
        fl.color = new Color(0.8f, 0.9f, 1f);
        fl.intensity = 3f;
        fl.range = range;
        fl.shadows = LightShadows.Soft;
        fl.shadowStrength = 0.3f;

        // Disable skybox
        RenderSettings.skybox = null;
        var cam = Camera.main;
        if (cam != null)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
        }

        // Ambient
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.55f);
    }

    GameObject BuildRoom(float size)
    {
        var room = new GameObject("Room");
        var half = size / 2f;

        var floorColor = new Color(0.75f, 0.75f, 0.78f);
        var wallColor = new Color(0.82f, 0.84f, 0.88f);
        var ceilingColor = new Color(0.9f, 0.9f, 0.92f);

        CreateWallQuad(room.transform, "Floor",   new Vector3(0, -half, 0), Quaternion.Euler(90, 0, 0),  new Vector2(size, size), floorColor, true);
        CreateWallQuad(room.transform, "Ceiling", new Vector3(0, half, 0),  Quaternion.Euler(-90, 0, 0), new Vector2(size, size), ceilingColor, true);
        CreateWallQuad(room.transform, "Left",    new Vector3(-half, 0, 0), Quaternion.Euler(0, -90, 0), new Vector2(size, size), wallColor, true);
        CreateWallQuad(room.transform, "Right",   new Vector3(half, 0, 0),  Quaternion.Euler(0, 90, 0),  new Vector2(size, size), wallColor, true);
        CreateWallQuad(room.transform, "Back",    new Vector3(0, 0, -half), Quaternion.Euler(0, 180, 0), new Vector2(size, size), wallColor, true);
        CreateWallQuad(room.transform, "Front",   new Vector3(0, 0, half),  Quaternion.Euler(0, 0, 0),   new Vector2(size, size), wallColor, true);

        return room;
    }

    static GameObject BuildConvexMeshVisual(ConvexMeshShape meshShape)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        var verts = meshShape.Vertices;

        if (meshShape.Triangles != null && meshShape.Triangles.Length > 0)
        {
            var srcVerts = meshShape.Triangles.Length;
            var flatVerts = new Vector3[srcVerts];
            var flatNormals = new Vector3[srcVerts];
            var flatTris = new int[srcVerts];

            for (var i = 0; i < meshShape.Triangles.Length; i += 3)
            {
                var a = new Vector3(verts[meshShape.Triangles[i]].X, verts[meshShape.Triangles[i]].Y, verts[meshShape.Triangles[i]].Z);
                var b = new Vector3(verts[meshShape.Triangles[i + 1]].X, verts[meshShape.Triangles[i + 1]].Y, verts[meshShape.Triangles[i + 1]].Z);
                var c = new Vector3(verts[meshShape.Triangles[i + 2]].X, verts[meshShape.Triangles[i + 2]].Y, verts[meshShape.Triangles[i + 2]].Z);
                var normal = Vector3.Cross(b - a, c - a).normalized;

                flatVerts[i] = a;
                flatVerts[i + 1] = b;
                flatVerts[i + 2] = c;
                flatNormals[i] = normal;
                flatNormals[i + 1] = normal;
                flatNormals[i + 2] = normal;
                flatTris[i] = i;
                flatTris[i + 1] = i + 1;
                flatTris[i + 2] = i + 2;
            }

            var mesh = new Mesh();
            mesh.vertices = flatVerts;
            mesh.normals = flatNormals;
            mesh.triangles = flatTris;
            mesh.RecalculateBounds();

            go.GetComponent<MeshFilter>().mesh = mesh;
        }
        else
        {
            var min = new Vector3(verts[0].X, verts[0].Y, verts[0].Z);
            var max = min;
            for (var i = 1; i < verts.Length; i++)
            {
                var v = new Vector3(verts[i].X, verts[i].Y, verts[i].Z);
                min = Vector3.Min(min, v);
                max = Vector3.Max(max, v);
            }
            go.transform.localScale = max - min;
        }

        return go;
    }

    void CreateWallQuad(Transform parent, string name, Vector3 pos, Quaternion rot, Vector2 size, Color color, bool receiveShadows)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);

        var col = go.GetComponent<Collider>();
        if (col != null) Destroy(col);

        var renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            ApplyMat(renderer, color, 0f, 0.15f);
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = receiveShadows;
        }
    }
}
