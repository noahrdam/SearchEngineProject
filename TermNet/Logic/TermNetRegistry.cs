using Shared.Model;

namespace TermNet.Logic;

public class TermNetRegistry
{
    private readonly List<ITermNet> _nets = new();

    public void Register(ITermNet net) => _nets.Add(net);

    public List<string> GetNames() => _nets.Select(n => n.Name).ToList();

    public List<SynonymEntry> GetSynonyms(string word, string[]? netNames = null)
    {
        var nets = netNames is { Length: > 0 }
            ? _nets.Where(n => netNames.Contains(n.Name))
            : _nets;

        var best = new Dictionary<string, double>();
        foreach (var net in nets)
            foreach (var (syn, weight) in net.GetSynonyms(word))
                if (!best.ContainsKey(syn) || best[syn] < weight)
                    best[syn] = weight;

        return best
            .Select(kv => new SynonymEntry { Word = kv.Key, Weight = kv.Value })
            .OrderByDescending(s => s.Weight)
            .ToList();
    }
}
