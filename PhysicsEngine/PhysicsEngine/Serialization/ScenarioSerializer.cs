using System.IO;
using System.Text.Json;

namespace PhysicsEngine.Serialization;

public static class ScenarioSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static void SaveToJson(ScenarioConfig config, string path)
    {
        var json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(path, json);
    }

    public static ScenarioConfig LoadFromJson(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<ScenarioConfig>(json)
               ?? throw new JsonException("Failed to deserialize ScenarioConfig");
    }
}
