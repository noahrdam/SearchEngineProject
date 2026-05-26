using System.Collections.Generic;
using Shared.Model;

namespace Indexer;

// Distributes documents across two database shards using docId mod 2.
// Words are written to both shards so each can resolve word lookups independently.
public class ReceptionDatabase : IDatabase
{
    private readonly IDatabase dbA;
    private readonly IDatabase dbB;

    public ReceptionDatabase(IDatabase a, IDatabase b)
    {
        dbA = a;
        dbB = b;
    }

    private IDatabase ShardFor(int docId) => docId % 2 == 0 ? dbA : dbB;

    public void InsertDocument(BEDocument doc) => ShardFor(doc.Id).InsertDocument(doc);

    public void InsertAllOcc(int docId, ISet<int> wordIds) => ShardFor(docId).InsertAllOcc(docId, wordIds);

    public void InsertWord(int id, string value)
    {
        dbA.InsertWord(id, value);
        dbB.InsertWord(id, value);
    }

    public void InsertAllWords(Dictionary<string, int> words)
    {
        dbA.InsertAllWords(words);
        dbB.InsertAllWords(words);
    }

    public Dictionary<string, int> GetAllWords() => dbA.GetAllWords();

    public int DocumentCounts => dbA.DocumentCounts + dbB.DocumentCounts;
}
