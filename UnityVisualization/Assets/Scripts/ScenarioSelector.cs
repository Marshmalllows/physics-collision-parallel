using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenarioSelector : MonoBehaviour
{
    public static string PendingScenario;
    public static SimParams? PendingParams;

    public struct SimParams
    {
        public float roomSize;
        public int sphereCount, boxCount, convexMeshCount;
        public float gravity, restitution, friction;
        public int solverIterations;
        public float linearDamping, angularDamping;
        public float minSpeed, maxSpeed;
        public bool useParallel;
    }

    SimulationRunner runner;
    string[] scenarioFiles;
    string[] displayNames;
    int selectedIndex;
    bool showDropdown;
    bool showPanel = true;

    string sRoomSize, sSpheres, sBoxes, sConvex;
    string sGravity, sRestitution, sFriction, sSolver;
    string sLinDamp, sAngDamp, sMinSpeed, sMaxSpeed;
    bool pUseParallel;

    GUIStyle headerStyle, labelStyle, fieldStyle, buttonStyle, toggleStyle;
    bool stylesReady;

    void Start()
    {
        runner = FindAnyObjectByType<SimulationRunner>();
        LoadScenarioList();

        selectedIndex = 0;
        if (runner != null && !string.IsNullOrEmpty(runner.scenarioFile))
        {
            for (int i = 0; i < scenarioFiles.Length; i++)
            {
                if (scenarioFiles[i] == runner.scenarioFile)
                {
                    selectedIndex = i + 1;
                    break;
                }
            }
        }

        ReadParamsFromRunner();
    }

    void ReadParamsFromRunner()
    {
        if (runner == null) return;
        sRoomSize = runner.roomSize.ToString("F0");
        sSpheres = runner.sphereCount.ToString();
        sBoxes = runner.boxCount.ToString();
        sConvex = runner.convexMeshCount.ToString();
        sGravity = runner.gravity.ToString("F2");
        sRestitution = runner.restitution.ToString("F2");
        sFriction = runner.friction.ToString("F2");
        sSolver = runner.solverIterations.ToString();
        sLinDamp = runner.linearDamping.ToString("F3");
        sAngDamp = runner.angularDamping.ToString("F3");
        sMinSpeed = runner.minSpeed.ToString("F1");
        sMaxSpeed = runner.maxSpeed.ToString("F1");
        pUseParallel = runner.useParallel;
    }

    void LoadScenarioList()
    {
        var scenariosDir = System.IO.Path.Combine(Application.streamingAssetsPath, "Scenarios");
        if (System.IO.Directory.Exists(scenariosDir))
        {
            var files = System.IO.Directory.GetFiles(scenariosDir, "*.json");
            scenarioFiles = new string[files.Length];
            displayNames = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                scenarioFiles[i] = System.IO.Path.GetFileName(files[i]);
                displayNames[i] = System.IO.Path.GetFileNameWithoutExtension(files[i]).Replace('_', ' ');
            }
            System.Array.Sort(displayNames, scenarioFiles);
        }
        else
        {
            scenarioFiles = new string[0];
            displayNames = new string[0];
        }
    }

    void InitStyles()
    {
        if (stylesReady) return;
        stylesReady = true;

        headerStyle = new GUIStyle(GUI.skin.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 13
        };

        labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 12 };

        fieldStyle = new GUIStyle(GUI.skin.textField)
        {
            fontSize = 12,
            fixedHeight = 20
        };

        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 12 };

        toggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 12 };
    }

    void OnGUI()
    {
        InitStyles();

        var panelWidth = 240f;
        var x = 10f;
        var y = 10f;

        // Toggle panel button
        if (GUI.Button(new Rect(x, y, 24, 24), showPanel ? "<" : ">", buttonStyle))
            showPanel = !showPanel;

        if (!showPanel) return;

        x += 30;

        var btnH = 24f;
        var current = selectedIndex == 0 ? "Scenario: random" : "Scenario: " + displayNames[selectedIndex - 1];
        if (GUI.Button(new Rect(x, y, panelWidth, btnH), showDropdown ? "\u25b2 " + current : "\u25bc " + current, buttonStyle))
            showDropdown = !showDropdown;

        if (showDropdown)
        {
            var totalItems = scenarioFiles.Length + 1;
            var dropY = y + btnH + 2;
            var dropHeight = totalItems * btnH;

            GUI.Box(new Rect(x, dropY, panelWidth, dropHeight), "");

            if (GUI.Button(new Rect(x, dropY, panelWidth, btnH),
                selectedIndex == 0 ? "\u2713 random (default)" : "  random (default)", buttonStyle))
            {
                selectedIndex = 0;
                showDropdown = false;
                ApplyScenario("");
            }

            for (var i = 0; i < scenarioFiles.Length; i++)
            {
                var btnY = dropY + btnH * (i + 1);
                var label = (selectedIndex == i + 1 ? "\u2713 " : "  ") + displayNames[i];
                if (GUI.Button(new Rect(x, btnY, panelWidth, btnH), label, buttonStyle))
                {
                    selectedIndex = i + 1;
                    showDropdown = false;
                    ApplyScenario(scenarioFiles[i]);
                }
            }
            return;
        }

        if (selectedIndex != 0)
        {
            y += btnH + 10;
            if (GUI.Button(new Rect(x, y, panelWidth, 28), "Restart", buttonStyle))
                Restart();
            return;
        }

        y += btnH + 10;
        var lw = 110f;
        var fw = panelWidth - lw - 4;
        var rowH = 22f;

        GUI.Label(new Rect(x, y, panelWidth, rowH), "Room & Bodies", headerStyle);
        y += rowH + 2;

        y = ParamRow(x, y, lw, fw, rowH, "Room size", ref sRoomSize);
        y = ParamRow(x, y, lw, fw, rowH, "Spheres", ref sSpheres);
        y = ParamRow(x, y, lw, fw, rowH, "Boxes", ref sBoxes);
        y = ParamRow(x, y, lw, fw, rowH, "Convex meshes", ref sConvex);

        y += 4;
        GUI.Label(new Rect(x, y, panelWidth, rowH), "Speed", headerStyle);
        y += rowH + 2;

        y = ParamRow(x, y, lw, fw, rowH, "Min speed", ref sMinSpeed);
        y = ParamRow(x, y, lw, fw, rowH, "Max speed", ref sMaxSpeed);

        y += 4;
        GUI.Label(new Rect(x, y, panelWidth, rowH), "Physics", headerStyle);
        y += rowH + 2;

        y = ParamRow(x, y, lw, fw, rowH, "Gravity", ref sGravity);
        y = ParamRow(x, y, lw, fw, rowH, "Restitution", ref sRestitution);
        y = ParamRow(x, y, lw, fw, rowH, "Friction", ref sFriction);
        y = ParamRow(x, y, lw, fw, rowH, "Solver iters", ref sSolver);
        y = ParamRow(x, y, lw, fw, rowH, "Linear damp", ref sLinDamp);
        y = ParamRow(x, y, lw, fw, rowH, "Angular damp", ref sAngDamp);

        y += 4;
        pUseParallel = GUI.Toggle(new Rect(x, y, panelWidth, rowH), pUseParallel, " Parallel physics", toggleStyle);
        y += rowH + 8;

        if (GUI.Button(new Rect(x, y, panelWidth, 28), "Restart", buttonStyle))
        {
            SaveParams();
            Restart();
        }
    }

    float ParamRow(float x, float y, float lw, float fw, float h, string label, ref string value)
    {
        GUI.Label(new Rect(x, y, lw, h), label, labelStyle);
        value = GUI.TextField(new Rect(x + lw, y, fw, h), value, fieldStyle);
        return y + h + 2;
    }

    void SaveParams()
    {
        float.TryParse(sRoomSize, out var roomSize);
        int.TryParse(sSpheres, out var spheres);
        int.TryParse(sBoxes, out var boxes);
        int.TryParse(sConvex, out var convex);
        float.TryParse(sGravity, out var grav);
        float.TryParse(sRestitution, out var rest);
        float.TryParse(sFriction, out var fric);
        int.TryParse(sSolver, out var solver);
        float.TryParse(sLinDamp, out var linD);
        float.TryParse(sAngDamp, out var angD);
        float.TryParse(sMinSpeed, out var minSpd);
        float.TryParse(sMaxSpeed, out var maxSpd);

        PendingParams = new SimParams
        {
            roomSize = roomSize > 0 ? roomSize : 20f,
            sphereCount = spheres,
            boxCount = boxes,
            convexMeshCount = convex,
            gravity = grav,
            restitution = rest,
            friction = fric,
            solverIterations = solver > 0 ? solver : 1,
            linearDamping = linD,
            angularDamping = angD,
            minSpeed = minSpd,
            maxSpeed = maxSpd,
            useParallel = pUseParallel
        };
    }

    void ApplyScenario(string fileName)
    {
        PendingScenario = fileName;
        PendingParams = null;
        Restart();
    }

    void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
