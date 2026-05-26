using System;
using System.Collections.Generic;
using Shared.Model;
using Npgsql;
using Shared;
namespace Indexer;

public class DatabasePostgres : IDatabase
{
    private readonly NpgsqlConnection _connection;

    public DatabasePostgres() : this(Paths.POSTGRES_DATABASE) { }

    public DatabasePostgres(string connectionString)
    {
        var csb = new NpgsqlConnectionStringBuilder(connectionString);
        // ensure port/host are parsed correctly
        if (string.IsNullOrEmpty(csb.Host)) csb.Host = "127.0.0.1";
        if (csb.Port == 0) csb.Port = 5432;

        _connection = new NpgsqlConnection(csb.ConnectionString);

        // Lazy-open: attempt open here but don't throw to caller (optionally fall back)
        try
        {
            _connection.Open();
        }
        catch (NpgsqlException ex)
        {
            // Log and rethrow or set a flag so callers can fall back
            throw new InvalidOperationException("Could not open Postgres connection. Ensure server is running and connection string is correct.", ex);
        }

        Execute("DROP TABLE IF EXISTS Occ");
        Execute("DROP TABLE IF EXISTS document");
        Execute("CREATE TABLE document(id INTEGER PRIMARY KEY, url TEXT, idxTime TIMESTAMP, creationTime TIMESTAMP, content TEXT)");
        Execute("DROP TABLE IF EXISTS word");
        Execute("CREATE TABLE word(id INTEGER PRIMARY KEY, name TEXT)");
        Execute("CREATE TABLE Occ(wordId INTEGER, docId INTEGER, "
                + "FOREIGN KEY (wordId) REFERENCES word(id), "
                + "FOREIGN KEY (docId) REFERENCES document(id))");
        Execute("CREATE INDEX word_index ON Occ (wordId)");
    }

    private void Execute(string sql)
    {
        var cmd = _connection.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    public void InsertAllWords(Dictionary<string, int> res)
    {
        if (res.Count == 0) return;
        using var writer = _connection.BeginBinaryImport("COPY word (id, name) FROM STDIN (FORMAT BINARY)");
        foreach (var p in res)
        {
            writer.StartRow();
            writer.Write(p.Value);
            writer.Write(p.Key);
        }
        writer.Complete();
    }

    public void InsertAllOcc(int docId, ISet<int> wordIds)
    {
        if (wordIds.Count == 0) return;
        using var writer = _connection.BeginBinaryImport("COPY occ (wordId, docId) FROM STDIN (FORMAT BINARY)");
        foreach (var wordId in wordIds)
        {
            writer.StartRow();
            writer.Write(wordId);
            writer.Write(docId);
        }
        writer.Complete();
    }

    public void InsertWord(int id, string value)
    {
        using var cmd = new NpgsqlCommand("INSERT INTO word(id, name) VALUES(@id,@name)", _connection);
        cmd.Parameters.AddWithValue("id", id);
        cmd.Parameters.AddWithValue("name", value);
        cmd.ExecuteNonQuery();
    }

    public void InsertDocument(BEDocument doc)
    {
        using var cmd = new NpgsqlCommand(
            "INSERT INTO document(id, url, idxTime, creationTime, content) VALUES(@id,@url,@idxTime,@creationTime,@content)",
            _connection);
        cmd.Parameters.AddWithValue("id", doc.Id);
        cmd.Parameters.AddWithValue("url", doc.Url);
        cmd.Parameters.AddWithValue("idxTime", doc.IdxTime);
        cmd.Parameters.AddWithValue("creationTime", doc.CreationTime);
        cmd.Parameters.AddWithValue("content", (object?)doc.Content ?? DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public Dictionary<string, int> GetAllWords()
    {
        Dictionary<string, int> res = new Dictionary<string, int>();

        var selectCmd = _connection.CreateCommand();
        selectCmd.CommandText = "SELECT * FROM word";

        using (var reader = selectCmd.ExecuteReader())
        {
            while (reader.Read())
            {
                var id = reader.GetInt32(0);
                var w = reader.GetString(1);

                res.Add(w, id);
            }
        }

        return res;
    }

    public int DocumentCounts
    {
        get
        {
            var selectCmd = _connection.CreateCommand();
            selectCmd.CommandText = "SELECT count(*) FROM document";

            using (var reader = selectCmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    var count = reader.GetInt32(0);
                    return count;
                }
            }

            return -1;
        }
    }
}