using System.Globalization;

namespace TermNet.Logic;

// Loads a termnet from a plain-text file. Each line: fromWord toWord weight
// Relations are bidirectional — looking up either end returns the other.
public class FileTermNet : ITermNet
{
    public string Name { get; }
    private readonly Dictionary<string, List<(string word, double weight)>> _relations = new();

    public FileTermNet(string name, string filePath)
    {
        Name = name;
        foreach (var line in File.ReadAllLines(filePath))
        {
            if (line.StartsWith('#') || string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(' ');
            if (parts.Length < 3) continue;
            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var weight)) continue;
            var from = parts[0].ToLower();
            var to = parts[1].ToLower();
            Add(from, to, weight);
            Add(to, from, weight);
        }
    }

    private void Add(string from, string to, double weight)
    {
        if (!_relations.ContainsKey(from)) _relations[from] = new();
        _relations[from].Add((to, weight));
    }

    public List<(string word, double weight)> GetSynonyms(string word) =>
        _relations.TryGetValue(word.ToLower(), out var syns) ? syns : new();

    public IEnumerable<(string from, List<(string word, double weight)> synonyms)> GetAllRelations() =>
        _relations.Select(kv => (kv.Key, kv.Value));
}
