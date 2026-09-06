using System.Text.Json;

namespace AutoComputer;

public sealed class AssistantMemory
{
    public string UserName { get; set; } = "";
    public string AssistantName { get; set; } = "AutoComputer";
    public List<string> Notes { get; set; } = [];
    public Dictionary<string, string> LearnedCommands { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static AssistantMemory Load(string path)
    {
        try
        {
            if (!File.Exists(path))
                return new AssistantMemory();

            var json = File.ReadAllText(path);
            var memory = JsonSerializer.Deserialize<AssistantMemory>(json) ?? new AssistantMemory();

            memory.LearnedCommands = new Dictionary<string, string>(
                memory.LearnedCommands ?? new Dictionary<string, string>(),
                StringComparer.OrdinalIgnoreCase);

            memory.Notes ??= [];
            return memory;
        }
        catch
        {
            return new AssistantMemory();
        }
    }

    public void Save(string path)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(path, JsonSerializer.Serialize(this, options));
    }
}
