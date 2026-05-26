using Shared.Model;

namespace SearchAPI.Logic;

public class CompositeDatabase : IDatabase
{
    private readonly IDatabase dbA;
    private readonly IDatabase dbB;

    public CompositeDatabase(IDatabase a, IDatabase b)
    {
        dbA = a;
        dbB = b;
    }

    public Dictionary<string, int> GetAllWords()
    {
        var a = dbA.GetAllWords();
        var b = dbB.GetAllWords();
        foreach (var kv in b)
            if (!a.ContainsKey(kv.Key))
                a.Add(kv.Key, kv.Value);
        return a;
    }

    public BEDocument GetDocDetails(int docId)
    {
        return dbA.GetDocDetails(docId) ?? dbB.GetDocDetails(docId);
    }

    public List<(int docId, int hits)> GetDocuments(List<int> wordIds, int maxAmount, int offset)
    {
        var fetch = maxAmount + offset;
        var taskA = Task.Run(() => dbA.GetDocuments(wordIds, fetch, 0));
        var taskB = Task.Run(() => dbB.GetDocuments(wordIds, fetch, 0));
        Task.WhenAll(taskA, taskB).Wait();
        var combined = new List<(int docId, int hits)>(taskA.Result);
        combined.AddRange(taskB.Result);
        combined.Sort((x, y) => y.hits.CompareTo(x.hits));
        return combined.Skip(offset).Take(maxAmount).ToList();
    }

    public int CountDocuments(List<int> wordIds)
    {
        var taskA = Task.Run(() => dbA.CountDocuments(wordIds));
        var taskB = Task.Run(() => dbB.CountDocuments(wordIds));
        Task.WhenAll(taskA, taskB).Wait();
        return taskA.Result + taskB.Result;
    }

    public string? GetFileContent(int docId) =>
        dbA.GetFileContent(docId) ?? dbB.GetFileContent(docId);

    public List<string> GetMissing(int docId, List<int> wordIds)
    {
        var r = dbA.GetMissing(docId, wordIds);
        return (r != null && r.Count > 0) ? r : dbB.GetMissing(docId, wordIds);
    }

    public List<string> GetHits(int docId, List<int> wordIds)
    {
        var r = dbA.GetHits(docId, wordIds);
        return (r != null && r.Count > 0) ? r : dbB.GetHits(docId, wordIds);
    }
}
