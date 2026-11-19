using System.Text.Json;

namespace ParameterQc;

public static class GroupDefaults
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    private static Dictionary<string, string>? _cache;

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ParameterQc",
        "default-groups.json");

    public static IReadOnlyDictionary<string, string> Load() => Load(FilePath);

    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        if (path == FilePath && _cache != null)
            return _cache;

        Dictionary<string, string> map = Read(path);
        if (path == FilePath)
            _cache = map;
        return map;
    }

    public static void Save(IEnumerable<ParameterSelection> assignments) => Save(FilePath, assignments);

    public static void Save(string path, IEnumerable<ParameterSelection> assignments)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (ParameterSelection assignment in assignments)
        {
            if (string.IsNullOrWhiteSpace(assignment.Name) || !ParameterGroups.IsAssignable(assignment.Group))
                continue;

            map[assignment.Name.Trim()] = assignment.Group;
        }

        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(folder))
            Directory.CreateDirectory(folder);

        File.WriteAllText(path, JsonSerializer.Serialize(map, Json));
        if (path == FilePath)
            _cache = map;
    }

    private static Dictionary<string, string> Read(string path)
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path))
            return map;

        try
        {
            Dictionary<string, string>? stored = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(path));
            if (stored == null)
                return map;

            foreach (KeyValuePair<string, string> pair in stored)
            {
                if (string.IsNullOrWhiteSpace(pair.Key) || !ParameterGroups.IsAssignable(pair.Value))
                    continue;

                map[pair.Key.Trim()] = pair.Value;
            }
        }
        catch (JsonException)
        {
            return map;
        }

        return map;
    }
}
