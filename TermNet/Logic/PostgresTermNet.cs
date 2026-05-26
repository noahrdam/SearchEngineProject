using Npgsql;
using Shared.Model;

namespace TermNet.Logic;

// Reads termnet relations from Postgres. Relations are stored unidirectionally
// (from_word → to_word) and queried in both directions.
public class PostgresTermNet : ITermNet
{
    public string Name { get; }
    private readonly string _connectionString;

    public PostgresTermNet(string name, string connectionString)
    {
        Name = name;
        _connectionString = connectionString;
    }

    public List<(string word, double weight)> GetSynonyms(string word)
    {
        var result = new List<(string, double)>();
        using var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT to_word, weight FROM synonym_relation WHERE net_name=@net AND from_word=@word
            UNION
            SELECT from_word, weight FROM synonym_relation WHERE net_name=@net AND to_word=@word";
        cmd.Parameters.AddWithValue("net", Name);
        cmd.Parameters.AddWithValue("word", word.ToLower());
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            result.Add((reader.GetString(0), reader.GetDouble(1)));
        return result;
    }

    // --- Schema + seeding ---

    public static void EnsureSchema(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS synonym_relation (
                net_name TEXT NOT NULL,
                from_word TEXT NOT NULL,
                to_word   TEXT NOT NULL,
                weight    FLOAT NOT NULL,
                PRIMARY KEY (net_name, from_word, to_word)
            )";
        cmd.ExecuteNonQuery();
    }

    public static bool IsEmpty(string connectionString)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM synonym_relation";
        return (long)cmd.ExecuteScalar()! == 0;
    }

    public static void Seed(string connectionString, IEnumerable<ITermNet> fileBased)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        foreach (var net in fileBased)
        {
            // FileTermNet stores both directions — we re-read from the file to get unique pairs.
            // Instead, ask each net for all its words via a known side-channel: cast to FileTermNet.
            if (net is not FileTermNet file) continue;
            foreach (var (fromWord, synonyms) in file.GetAllRelations())
            {
                foreach (var (toWord, weight) in synonyms)
                {
                    // only insert canonical direction (from < to alphabetically) to avoid duplicates
                    if (string.Compare(fromWord, toWord, StringComparison.Ordinal) >= 0) continue;
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO synonym_relation (net_name, from_word, to_word, weight)
                        VALUES (@net, @from, @to, @w)
                        ON CONFLICT DO NOTHING";
                    cmd.Parameters.AddWithValue("net", net.Name);
                    cmd.Parameters.AddWithValue("from", fromWord);
                    cmd.Parameters.AddWithValue("to", toWord);
                    cmd.Parameters.AddWithValue("w", weight);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
