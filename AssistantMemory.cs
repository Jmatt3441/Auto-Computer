using System.Text.Json;

namespace AutoComputer;

public sealed class AssistantMemory
{
    public string UserName { get; set; } = "";
    public string AssistantName { get; set; } = "Hal";
    public List<string> Notes { get; set; } = [];

    public static AssistantMemory Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AssistantMemory();

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AssistantMemory>(json) ?? new AssistantMemory();
        }
        catch
        {
            return new AssistantMemory();
        }
    }

    public void Save(string path)
    {
        try
        {
            string? folder = Path.GetDirectoryName(path);
            if (!string.IsNullOrWhiteSpace(folder))
                Directory.CreateDirectory(folder);

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(this, options));
        }
        catch
        {
        }
    }
}
